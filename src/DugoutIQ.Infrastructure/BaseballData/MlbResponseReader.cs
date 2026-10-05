using System.Globalization;
using System.Text.Json;
using DugoutIQ.Application.Baseball;
using DugoutIQ.Application.Players;
using DugoutIQ.Domain.Enums;

namespace DugoutIQ.Infrastructure.BaseballData;

internal static class MlbResponseReader
{
    public static IReadOnlyList<ExternalPlayer> ReadSearch(string json)
    {
        var envelope = JsonSerializer.Deserialize<MlbPeopleEnvelope>(json, MlbJson.Options);
        if (envelope is null)
        {
            return [];
        }

        return envelope.People
            .Where(person => person.IsPlayer != false)
            .Select(MapPlayer)
            .Where(player => player is not null)
            .Select(player => player!)
            .Take(PlayerSearchCriteria.MaximumResults)
            .ToArray();
    }

    public static ExternalPlayer? ReadPlayer(string json)
    {
        var envelope = JsonSerializer.Deserialize<MlbPeopleEnvelope>(json, MlbJson.Options);
        var person = envelope?.People.FirstOrDefault();
        return person is null ? null : MapPlayer(person);
    }

    public static int? ReadCurrentTeamId(string json)
    {
        var envelope = JsonSerializer.Deserialize<MlbPeopleEnvelope>(json, MlbJson.Options);
        var teamId = envelope?.People.FirstOrDefault()?.CurrentTeam?.Id;
        return teamId is > 0 ? teamId : null;
    }

    public static ExternalTeam? ReadTeam(string json)
    {
        var envelope = JsonSerializer.Deserialize<MlbTeamsEnvelope>(json, MlbJson.Options);
        var team = envelope?.Teams.FirstOrDefault();
        if (team is null || team.Id <= 0 || string.IsNullOrWhiteSpace(team.Name))
        {
            return null;
        }

        var league = ParseLeague(team.League?.Name);
        var abbreviation = string.IsNullOrWhiteSpace(team.Abbreviation)
            ? "UNK"
            : team.Abbreviation.Trim().ToUpperInvariant();

        return new ExternalTeam(
            team.Id,
            team.Name.Trim(),
            abbreviation,
            league,
            NormalizeDivision(team.Division?.Name));
    }

    public static ExternalSeasonHitting? ReadSeasonHitting(string json, int season)
    {
        var splits = ReadRegularSeasonSplits(json, season);
        if (splits.Count == 0)
        {
            return null;
        }

        var games = splits.Sum(split => ReadInt(split.Stat, "gamesPlayed"));
        var plateAppearances = splits.Sum(split => ReadInt(split.Stat, "plateAppearances"));
        var atBats = splits.Sum(split => ReadInt(split.Stat, "atBats"));
        if (games == 0 && plateAppearances == 0 && atBats == 0)
        {
            return null;
        }

        return new ExternalSeasonHitting(
            season,
            games,
            plateAppearances,
            atBats,
            splits.Sum(split => ReadInt(split.Stat, "hits")),
            splits.Sum(split => ReadInt(split.Stat, "doubles")),
            splits.Sum(split => ReadInt(split.Stat, "triples")),
            splits.Sum(split => ReadInt(split.Stat, "homeRuns")),
            splits.Sum(split => ReadInt(split.Stat, "baseOnBalls")),
            splits.Sum(split => ReadInt(split.Stat, "strikeOuts")),
            splits.Sum(split => ReadInt(split.Stat, "hitByPitch")),
            splits.Sum(split => ReadInt(split.Stat, "sacFlies")));
    }

    public static ExternalExpectedHitting? ReadExpectedHitting(string json, int season)
    {
        var splits = ReadRegularSeasonSplits(json, season);
        if (splits.Count == 0)
        {
            return null;
        }

        // Expected rates are season-level, not counting stats. If a player changed
        // clubs, the provider may return more than one split; use the last regular-season split
        // rather than averaging rates that have different sample sizes.
        var stat = splits[^1].Stat;
        var expected = new ExternalExpectedHitting(
            season,
            ReadDecimal(stat, "avg"),
            ReadDecimal(stat, "slg"),
            ReadDecimal(stat, "woba"));

        if (expected.ExpectedBattingAverage is null
            && expected.ExpectedSlugging is null
            && expected.ExpectedWoba is null)
        {
            return null;
        }

        return expected;
    }

    private static List<MlbSplit> ReadRegularSeasonSplits(string json, int season)
    {
        var envelope = JsonSerializer.Deserialize<MlbStatsEnvelope>(json, MlbJson.Options);
        if (envelope is null)
        {
            return [];
        }

        var seasonText = season.ToString(CultureInfo.InvariantCulture);
        return envelope.Stats
            .SelectMany(block => block.Splits)
            .Where(split =>
                string.Equals(split.Season, seasonText, StringComparison.Ordinal)
                && (string.IsNullOrEmpty(split.GameType) || string.Equals(split.GameType, "R", StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static ExternalPlayer? MapPlayer(MlbPerson person)
    {
        if (person.Id <= 0
            || string.IsNullOrWhiteSpace(person.FirstName)
            || string.IsNullOrWhiteSpace(person.LastName))
        {
            return null;
        }

        DateOnly? birthDate = null;
        if (DateOnly.TryParse(person.BirthDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            birthDate = parsed;
        }

        var position = person.PrimaryPosition?.Abbreviation;
        if (string.IsNullOrWhiteSpace(position))
        {
            position = "UNK";
        }

        return new ExternalPlayer(
            person.Id,
            person.FirstName.Trim(),
            person.LastName.Trim(),
            position.Trim().ToUpperInvariant(),
            ParseBatting(person.BatSide?.Code),
            ParseThrowing(person.PitchHand?.Code),
            birthDate);
    }

    private static BattingSide ParseBatting(string? code) => code switch
    {
        "R" => BattingSide.Right,
        "L" => BattingSide.Left,
        "S" => BattingSide.Switch,
        _ => BattingSide.Unknown
    };

    private static ThrowingArm ParseThrowing(string? code) => code switch
    {
        "R" => ThrowingArm.Right,
        "L" => ThrowingArm.Left,
        _ => ThrowingArm.Unknown
    };

    private static League ParseLeague(string? name) => name switch
    {
        "American League" => League.American,
        "National League" => League.National,
        _ => League.Unknown
    };

    private static string NormalizeDivision(string? divisionName)
    {
        if (string.IsNullOrWhiteSpace(divisionName))
        {
            return "Unknown";
        }

        const string american = "American League ";
        const string national = "National League ";
        if (divisionName.StartsWith(american, StringComparison.Ordinal))
        {
            return divisionName[american.Length..].Trim();
        }

        if (divisionName.StartsWith(national, StringComparison.Ordinal))
        {
            return divisionName[national.Length..].Trim();
        }

        return divisionName.Trim();
    }

    private static int ReadInt(JsonElement stat, string name)
    {
        if (stat.ValueKind != JsonValueKind.Object || !stat.TryGetProperty(name, out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return 0;
    }

    private static decimal? ReadDecimal(JsonElement stat, string name)
    {
        if (stat.ValueKind != JsonValueKind.Object || !stat.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
