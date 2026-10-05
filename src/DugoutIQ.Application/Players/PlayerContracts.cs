using DugoutIQ.Application.Exceptions;

namespace DugoutIQ.Application.Players;

public static class PlayerSearchCriteria
{
    public const int MinimumQueryLength = 2;
    public const int MaximumQueryLength = 80;
    public const int MaximumResults = 25;

    public static string Normalize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new RequestValidationException("q", "Enter a player name.");
        }

        var trimmed = query.Trim();
        if (trimmed.Length < MinimumQueryLength)
        {
            throw new RequestValidationException("q", $"Enter at least {MinimumQueryLength} characters.");
        }

        if (trimmed.Length > MaximumQueryLength)
        {
            throw new RequestValidationException("q", $"Enter at most {MaximumQueryLength} characters.");
        }

        return trimmed;
    }
}

public sealed record PlayerSearchResultDto(
    Guid Id,
    int ExternalId,
    string FirstName,
    string LastName,
    string Position,
    string Bats,
    string Throws,
    string? TeamAbbreviation,
    string? TeamName);

public sealed record TeamSummaryDto(
    Guid Id,
    string Name,
    string Abbreviation,
    string League,
    string Division);

public sealed record SeasonHittingDto(
    int Season,
    int Games,
    int PlateAppearances,
    int AtBats,
    int Hits,
    int Doubles,
    int Triples,
    int HomeRuns,
    int Walks,
    int Strikeouts,
    int HitByPitch,
    int SacrificeFlies,
    decimal? BattingAverage,
    decimal? OnBasePercentage,
    decimal? SluggingPercentage,
    decimal? Ops,
    DateTimeOffset UpdatedAt,
    string Freshness,
    string RateStatSource);

public sealed record ExpectedHittingDto(
    int Season,
    decimal? ExpectedBattingAverage,
    decimal? ExpectedSlugging,
    decimal? ExpectedWoba,
    DateTimeOffset CapturedAt,
    string Source);

public sealed record UnavailableMetricDto(string Name, string Reason);

public sealed record PlayerProfileDto(
    Guid Id,
    int ExternalId,
    string FirstName,
    string LastName,
    string Position,
    string Bats,
    string Throws,
    DateOnly? BirthDate,
    TeamSummaryDto? Team,
    SeasonHittingDto? CurrentSeason,
    ExpectedHittingDto? ExpectedHitting,
    IReadOnlyList<UnavailableMetricDto> UnavailableMetrics,
    string DataClassification);
