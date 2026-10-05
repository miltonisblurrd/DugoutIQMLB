using System.Diagnostics;
using System.Net;
using System.Text.Json;
using DugoutIQ.Application.Abstractions;
using DugoutIQ.Application.Baseball;
using DugoutIQ.Application.Exceptions;
using DugoutIQ.Application.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DugoutIQ.Infrastructure.BaseballData;

public sealed class MlbBaseballDataProvider : IBaseballDataProvider
{
    public const string HttpClientName = "MlbStatsApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly BaseballDataOptions _options;
    private readonly ILogger<MlbBaseballDataProvider> _logger;

    public MlbBaseballDataProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<BaseballDataOptions> options,
        ILogger<MlbBaseballDataProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalPlayer>> SearchPlayersAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"mlb:search:{query.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ExternalPlayer>? cached) && cached is not null)
        {
            _logger.LogInformation(
                "ExternalSearchCacheHit ExternalProvider={ExternalProvider} CacheHit={CacheHit} Query={Query}",
                HttpClientName,
                true,
                query);
            return cached;
        }

        var path = $"api/v1/people/search?names={Uri.EscapeDataString(query)}&sportIds=1";
        var json = await GetStringAsync(path, "SearchPlayers", cancellationToken);
        var players = json is null ? [] : MlbResponseReader.ReadSearch(json);

        if (_options.SearchCacheSeconds > 0)
        {
            _cache.Set(cacheKey, players, TimeSpan.FromSeconds(_options.SearchCacheSeconds));
        }

        _logger.LogInformation(
            "ExternalSearchCompleted ExternalProvider={ExternalProvider} CacheHit={CacheHit} Query={Query} ResultCount={ResultCount}",
            HttpClientName,
            false,
            query,
            players.Count);

        return players;
    }

    public async Task<ExternalPlayerProfile?> GetPlayerAsync(int externalId, CancellationToken cancellationToken)
    {
        var path = $"api/v1/people/{externalId.ToString(System.Globalization.CultureInfo.InvariantCulture)}?hydrate=currentTeam";
        var json = await GetStringAsync(path, "GetPlayer", cancellationToken);
        if (json is null)
        {
            return null;
        }

        var player = MlbResponseReader.ReadPlayer(json);
        if (player is null)
        {
            return null;
        }

        ExternalTeam? team = null;
        var teamId = MlbResponseReader.ReadCurrentTeamId(json);
        if (teamId is not null)
        {
            var teamJson = await GetStringAsync($"api/v1/teams/{teamId.Value}", "GetTeam", cancellationToken);
            if (teamJson is not null)
            {
                team = MlbResponseReader.ReadTeam(teamJson);
            }
        }

        return new ExternalPlayerProfile(player, team);
    }

    public async Task<ExternalSeasonHitting?> GetSeasonHittingAsync(
        int externalId,
        int season,
        CancellationToken cancellationToken)
    {
        var path = StatsPath(externalId, season, "season");
        var json = await GetStringAsync(path, "GetSeasonHitting", cancellationToken);
        return json is null ? null : MlbResponseReader.ReadSeasonHitting(json, season);
    }

    public async Task<ExternalExpectedHitting?> GetExpectedHittingAsync(
        int externalId,
        int season,
        CancellationToken cancellationToken)
    {
        var path = StatsPath(externalId, season, "expectedStatistics");
        var json = await GetStringAsync(path, "GetExpectedHitting", cancellationToken);
        return json is null ? null : MlbResponseReader.ReadExpectedHitting(json, season);
    }

    private static string StatsPath(int externalId, int season, string stats) =>
        $"api/v1/people/{externalId}/stats?stats={stats}&group=hitting&season={season}&sportId=1&gameType=R";

    private async Task<string?> GetStringAsync(string relativePath, string operation, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var started = Stopwatch.GetTimestamp();

        try
        {
            using var response = await client.GetAsync(relativePath, cancellationToken);
            var duration = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation(
                    "ExternalProviderCompleted ExternalProvider={ExternalProvider} Operation={Operation} StatusCode={StatusCode} Duration={Duration}",
                    HttpClientName,
                    operation,
                    (int)response.StatusCode,
                    duration);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ExternalProviderFailed ExternalProvider={ExternalProvider} Operation={Operation} StatusCode={StatusCode} Duration={Duration}",
                    HttpClientName,
                    operation,
                    (int)response.StatusCode,
                    duration);
                throw new ExternalProviderException(
                    HttpClientName,
                    operation,
                    "The baseball data provider returned an error.");
            }

            _logger.LogInformation(
                "ExternalProviderCompleted ExternalProvider={ExternalProvider} Operation={Operation} StatusCode={StatusCode} Duration={Duration}",
                HttpClientName,
                operation,
                (int)response.StatusCode,
                duration);

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ExternalProviderException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            var duration = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            _logger.LogWarning(
                exception,
                "ExternalProviderFailed ExternalProvider={ExternalProvider} Operation={Operation} Duration={Duration}",
                HttpClientName,
                operation,
                duration);
            throw new ExternalProviderException(
                HttpClientName,
                operation,
                "The baseball data provider could not be reached.",
                exception);
        }
    }
}
