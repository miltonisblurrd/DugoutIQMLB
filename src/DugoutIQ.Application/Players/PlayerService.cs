using DugoutIQ.Application.Abstractions;
using DugoutIQ.Application.Baseball;
using DugoutIQ.Application.Exceptions;
using DugoutIQ.Application.Options;
using DugoutIQ.Domain.Entities;
using DugoutIQ.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DugoutIQ.Application.Players;

public sealed class PlayerService : IPlayerService
{
    public const string RateStatSource = "DugoutIQ-derived from persisted counting stats";
    public const string StoredExpectedSource = "Persisted DugoutIQ snapshot of MLB Stats API expected statistics";
    public const string DataClassification =
        "Identity and counting stats are persisted from the MLB Stats API. " +
        "AVG, OBP, SLG, and OPS are calculated by DugoutIQ. " +
        "Expected statistics are provider observations, stored when returned. " +
        "This is not demo data.";

    private static readonly IReadOnlyList<UnavailableMetricDto> UnavailableMetrics =
    [
        new("Average exit velocity", "Not supplied by the MLB Stats API provider."),
        new("Max exit velocity", "Not supplied by the MLB Stats API provider."),
        new("Barrel rate", "Not supplied by the MLB Stats API provider."),
        new("Hard-hit rate", "Not supplied by the MLB Stats API provider."),
        new("Launch angle", "Not supplied by the MLB Stats API provider."),
        new("Whiff rate", "Not supplied by the MLB Stats API provider."),
        new("Chase rate", "Not supplied by the MLB Stats API provider.")
    ];

    private readonly IPlayerRepository _players;
    private readonly IBaseballDataProvider _baseballData;
    private readonly IMemoryCache _cache;
    private readonly BaseballDataOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(
        IPlayerRepository players,
        IBaseballDataProvider baseballData,
        IMemoryCache cache,
        IOptions<BaseballDataOptions> options,
        TimeProvider time,
        ILogger<PlayerService> logger)
    {
        _players = players;
        _baseballData = baseballData;
        _cache = cache;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PlayerSearchResultDto>> SearchAsync(
        string? query,
        CancellationToken cancellationToken)
    {
        var normalized = PlayerSearchCriteria.Normalize(query);
        var local = await _players.SearchByNameAsync(
            normalized,
            PlayerSearchCriteria.MaximumResults,
            cancellationToken);

        if (local.Count > 0)
        {
            _logger.LogInformation(
                "PlayerSearchCompleted Query={Query} ResultCount={ResultCount} ProviderCalled={ProviderCalled}",
                normalized,
                local.Count,
                false);
            return local.Select(MapSearchResult).ToArray();
        }

        // No local match. The provider exception is already a safe application error;
        // let it reach the exception handler instead of wrapping it with provider internals.
        var external = await _baseballData.SearchPlayersAsync(normalized, cancellationToken);
        if (external.Count == 0)
        {
            _logger.LogInformation(
                "PlayerSearchCompleted Query={Query} ResultCount={ResultCount} ProviderCalled={ProviderCalled}",
                normalized,
                0,
                true);
            return [];
        }

        var persisted = await _players.UpsertPlayersAsync(external, _time.GetUtcNow(), cancellationToken);
        var results = persisted
            .OrderBy(player => player.LastName)
            .ThenBy(player => player.FirstName)
            .Take(PlayerSearchCriteria.MaximumResults)
            .Select(MapSearchResult)
            .ToArray();

        _logger.LogInformation(
            "PlayerSearchCompleted Query={Query} ResultCount={ResultCount} ProviderCalled={ProviderCalled}",
            normalized,
            results.Length,
            true);

        return results;
    }

    public async Task<PlayerProfileDto> GetProfileAsync(Guid playerId, CancellationToken cancellationToken)
    {
        if (playerId == Guid.Empty)
        {
            throw new RequestValidationException("id", "Player id is required.");
        }

        var season = _options.ResolveSeason(DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime));
        var cacheKey = $"player:{playerId:D}:profile:{season}";
        if (_cache.TryGetValue(cacheKey, out PlayerProfileDto? cached) && cached is not null)
        {
            _logger.LogInformation(
                "PlayerProfileRetrieved PlayerId={PlayerId} CacheHit={CacheHit}",
                playerId,
                true);
            return cached;
        }

        var player = await _players.GetByIdAsync(playerId, cancellationToken);
        if (player is null)
        {
            throw new NotFoundException("Player", playerId.ToString());
        }

        var refreshFailed = false;
        var currentSeason = player.Seasons.FirstOrDefault(row => row.Season == season);
        var refreshAfter = _time.GetUtcNow().AddHours(-_options.SeasonRefreshHours);
        if (currentSeason is null || currentSeason.UpdatedAt < refreshAfter)
        {
            refreshFailed = !await TryRefreshAsync(player, season, refreshAfter, cancellationToken);
            currentSeason = player.Seasons.FirstOrDefault(row => row.Season == season);
        }

        var latestSnapshot = await _players.GetLatestSnapshotAsync(player.Id, cancellationToken);
        var profile = MapProfile(player, season, currentSeason, latestSnapshot, refreshFailed);

        if (_options.ProfileCacheSeconds > 0)
        {
            _cache.Set(cacheKey, profile, TimeSpan.FromSeconds(_options.ProfileCacheSeconds));
        }

        _logger.LogInformation(
            "PlayerProfileRetrieved PlayerId={PlayerId} CacheHit={CacheHit} Season={Season}",
            player.Id,
            false,
            season);

        return profile;
    }

    private async Task<bool> TryRefreshAsync(
        Player player,
        int season,
        DateTimeOffset refreshAfter,
        CancellationToken cancellationToken)
    {
        ExternalPlayerProfile? detail;
        ExternalSeasonHitting? hitting;
        ExternalExpectedHitting? expected;

        try
        {
            detail = await _baseballData.GetPlayerAsync(player.ExternalId, cancellationToken);
            hitting = await _baseballData.GetSeasonHittingAsync(player.ExternalId, season, cancellationToken);
            expected = await _baseballData.GetExpectedHittingAsync(player.ExternalId, season, cancellationToken);
        }
        catch (ExternalProviderException exception)
        {
            _logger.LogWarning(
                exception,
                "PlayerProfileRefreshFailed PlayerId={PlayerId} ExternalProvider={ExternalProvider} Operation={Operation}",
                player.Id,
                exception.Provider,
                exception.Operation);
            return false;
        }

        var utcNow = _time.GetUtcNow();
        if (detail is not null)
        {
            var incoming = detail.Player;
            player.ApplyProfile(
                incoming.FirstName,
                incoming.LastName,
                incoming.PositionAbbreviation,
                incoming.Bats,
                incoming.Throws,
                incoming.BirthDate,
                utcNow);

            if (detail.CurrentTeam is not null)
            {
                var team = await _players.UpsertTeamAsync(detail.CurrentTeam, cancellationToken);
                player.AssignTeam(team, utcNow);
            }
        }

        if (hitting is not null)
        {
            await _players.UpsertSeasonAsync(player, hitting, utcNow, cancellationToken);
        }

        if (HasExpectedValue(expected))
        {
            var latest = await _players.GetLatestSnapshotAsync(player.Id, cancellationToken);
            var snapshotIsCurrent = latest is not null
                && latest.Season == season
                && latest.CapturedAt >= refreshAfter;

            if (!snapshotIsCurrent)
            {
                _players.AddSnapshot(StatcastSnapshot.CaptureExpectedHitting(
                    player.Id,
                    season,
                    expected!.ExpectedBattingAverage,
                    expected.ExpectedSlugging,
                    expected.ExpectedWoba,
                    utcNow));
            }
        }

        await _players.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool HasExpectedValue(ExternalExpectedHitting? expected) =>
        expected is not null
        && (expected.ExpectedBattingAverage is not null
            || expected.ExpectedSlugging is not null
            || expected.ExpectedWoba is not null);

    private static PlayerSearchResultDto MapSearchResult(Player player) =>
        new(
            player.Id,
            player.ExternalId,
            player.FirstName,
            player.LastName,
            player.PositionAbbreviation,
            FormatBats(player.Bats),
            FormatThrow(player.Throws),
            player.Team?.Abbreviation,
            player.Team?.Name);

    private static PlayerProfileDto MapProfile(
        Player player,
        int season,
        PlayerSeason? currentSeason,
        StatcastSnapshot? latestSnapshot,
        bool refreshFailed)
    {
        ExpectedHittingDto? expected = null;
        if (latestSnapshot is not null && latestSnapshot.Season == season)
        {
            expected = new ExpectedHittingDto(
                latestSnapshot.Season,
                latestSnapshot.ExpectedBattingAverage,
                latestSnapshot.ExpectedSlugging,
                latestSnapshot.ExpectedWoba,
                latestSnapshot.CapturedAt,
                StoredExpectedSource);
        }

        return new PlayerProfileDto(
            player.Id,
            player.ExternalId,
            player.FirstName,
            player.LastName,
            player.PositionAbbreviation,
            FormatBats(player.Bats),
            FormatThrow(player.Throws),
            player.BirthDate,
            player.Team is null ? null : MapTeam(player.Team),
            currentSeason is null ? null : MapSeason(currentSeason, refreshFailed),
            expected,
            UnavailableMetrics,
            DataClassification);
    }

    private static TeamSummaryDto MapTeam(Team team) =>
        new(
            team.Id,
            team.Name,
            team.Abbreviation,
            FormatLeague(team.League),
            team.Division);

    private static SeasonHittingDto MapSeason(PlayerSeason season, bool refreshFailed)
    {
        var rates = season.Rates;
        var freshness = refreshFailed
            ? "Showing the last persisted season. A refresh from the baseball provider failed."
            : "Persisted counting stats.";

        return new SeasonHittingDto(
            season.Season,
            season.Games,
            season.PlateAppearances,
            season.AtBats,
            season.Hits,
            season.Doubles,
            season.Triples,
            season.HomeRuns,
            season.Walks,
            season.Strikeouts,
            season.HitByPitch,
            season.SacrificeFlies,
            rates.BattingAverage,
            rates.OnBasePercentage,
            rates.SluggingPercentage,
            rates.Ops,
            season.UpdatedAt,
            freshness,
            RateStatSource);
    }

    private static string FormatBats(BattingSide bats) => bats switch
    {
        BattingSide.Right => "R",
        BattingSide.Left => "L",
        BattingSide.Switch => "S",
        _ => "?"
    };

    private static string FormatThrow(ThrowingArm throwsArm) => throwsArm switch
    {
        ThrowingArm.Right => "R",
        ThrowingArm.Left => "L",
        _ => "?"
    };

    private static string FormatLeague(League league) => league switch
    {
        League.American => "AL",
        League.National => "NL",
        _ => "Unknown"
    };
}
