using DugoutIQ.Application.Abstractions;
using DugoutIQ.Application.Baseball;
using DugoutIQ.Application.Exceptions;
using DugoutIQ.Application.Options;
using DugoutIQ.Application.Players;
using DugoutIQ.Domain.Entities;
using DugoutIQ.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DugoutIQ.UnitTests;

public class PlayerServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 29, 16, 0, 0, TimeSpan.Zero);

    private readonly Mock<IPlayerRepository> _players = new();
    private readonly Mock<IBaseballDataProvider> _baseball = new();
    private readonly PlayerService _service;

    public PlayerServiceTests()
    {
        var options = Options.Create(new BaseballDataOptions
        {
            CurrentSeason = 2026,
            SeasonRefreshHours = 6,
            ProfileCacheSeconds = 0
        });

        _players
            .Setup(repository => repository.GetLatestSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StatcastSnapshot?)null);
        _players
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new PlayerService(
            _players.Object,
            _baseball.Object,
            new MissCache(),
            options,
            new FixedTime(Now),
            Mock.Of<ILogger<PlayerService>>());
    }

    [Fact]
    public async Task Search_WhenLocalMatchExists_DoesNotCallProvider()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var player = SamplePlayer();
        _players
            .Setup(repository => repository.SearchByNameAsync("Aaron Judge", 25, token))
            .ReturnsAsync([player]);

        var results = await _service.SearchAsync("  Aaron Judge  ", token);

        var result = Assert.Single(results);
        Assert.Equal(player.Id, result.Id);
        Assert.Equal("Aaron", result.FirstName);
        Assert.Equal("Judge", result.LastName);
        Assert.Equal("RF", result.Position);
        Assert.Equal("R", result.Bats);
        Assert.Equal("R", result.Throws);
        _baseball.Verify(
            provider => provider.SearchPlayersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _players.Verify(
            repository => repository.UpsertPlayersAsync(
                It.IsAny<IReadOnlyList<ExternalPlayer>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Search_WhenLocalMisses_PersistsProviderPlayers()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var external = new ExternalPlayer(
            592450,
            "Aaron",
            "Judge",
            "RF",
            BattingSide.Right,
            ThrowingArm.Right,
            new DateOnly(1992, 4, 26));
        var persisted = SamplePlayer();

        _players
            .Setup(repository => repository.SearchByNameAsync("Aaron Judge", 25, token))
            .ReturnsAsync([]);
        _baseball
            .Setup(provider => provider.SearchPlayersAsync("Aaron Judge", token))
            .ReturnsAsync([external]);
        _players
            .Setup(repository => repository.UpsertPlayersAsync(
                It.Is<IReadOnlyList<ExternalPlayer>>(players => players.Count == 1 && players[0].ExternalId == 592450),
                Now,
                token))
            .ReturnsAsync([persisted]);

        var results = await _service.SearchAsync("Aaron Judge", token);

        var result = Assert.Single(results);
        Assert.Equal(persisted.Id, result.Id);
        Assert.Equal(592450, result.ExternalId);
        Assert.Equal("Aaron", result.FirstName);
        Assert.Equal("Judge", result.LastName);
        Assert.Equal("RF", result.Position);
    }

    [Fact]
    public async Task Search_WhenProviderHasNoMatch_ReturnsEmptyWithoutPersisting()
    {
        _players
            .Setup(repository => repository.SearchByNameAsync("Nobody", 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _baseball
            .Setup(provider => provider.SearchPlayersAsync("Nobody", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var results = await _service.SearchAsync("Nobody", CancellationToken.None);

        Assert.Empty(results);
        _players.Verify(
            repository => repository.UpsertPlayersAsync(
                It.IsAny<IReadOnlyList<ExternalPlayer>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Search_WhenProviderFails_ThrowsSafeApplicationError()
    {
        var secret = new HttpRequestException("timeout talking to statsapi.mlb.com from pod internal-net");
        var providerFailure = new ExternalProviderException(
            "MlbStatsApi",
            "SearchPlayers",
            "The baseball data provider could not be reached.",
            secret);

        _players
            .Setup(repository => repository.SearchByNameAsync("Aaron Judge", 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _baseball
            .Setup(provider => provider.SearchPlayersAsync("Aaron Judge", It.IsAny<CancellationToken>()))
            .ThrowsAsync(providerFailure);

        var exception = await Assert.ThrowsAsync<ExternalProviderException>(() =>
            _service.SearchAsync("Aaron Judge", CancellationToken.None));

        Assert.Equal("The baseball data provider could not be reached.", exception.Message);
        Assert.DoesNotContain("statsapi", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpRequestException", exception.Message, StringComparison.Ordinal);
        _players.Verify(
            repository => repository.UpsertPlayersAsync(
                It.IsAny<IReadOnlyList<ExternalPlayer>>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Search_WhenQueryIsTooShort_ThrowsBeforeRepositoryOrProvider()
    {
        var exception = await Assert.ThrowsAsync<RequestValidationException>(() =>
            _service.SearchAsync("A", CancellationToken.None));

        Assert.Equal("q", Assert.Single(exception.Errors.Keys));
        _players.Verify(
            repository => repository.SearchByNameAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _baseball.Verify(
            provider => provider.SearchPlayersAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetProfile_WhenSeasonIsFresh_DoesNotCallProvider()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var player = SamplePlayer();
        player.UpsertSeason(2026, 66, 285, 237, 57, 10, 1, 18, 43, 85, 2, 1, Now);
        _players.Setup(repository => repository.GetByIdAsync(player.Id, token)).ReturnsAsync(player);

        var profile = await _service.GetProfileAsync(player.Id, token);

        Assert.Equal("Aaron", profile.FirstName);
        Assert.Equal(0.241m, profile.CurrentSeason!.BattingAverage);
        _baseball.Verify(
            provider => provider.GetPlayerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _baseball.Verify(
            provider => provider.GetSeasonHittingAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetProfile_WhenPlayerIsMissing_ThrowsNotFoundWithoutCallingProvider()
    {
        var id = Guid.NewGuid();
        _players
            .Setup(repository => repository.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Player?)null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetProfileAsync(id, CancellationToken.None));

        Assert.Contains(id.ToString(), exception.Message, StringComparison.Ordinal);
        _baseball.Verify(
            provider => provider.GetPlayerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetProfile_WhenSeasonIsMissing_PersistsProviderLine()
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var player = SamplePlayer();
        var hitting = new ExternalSeasonHitting(2026, 66, 285, 237, 57, 10, 1, 18, 43, 85, 2, 1);

        _players.Setup(repository => repository.GetByIdAsync(player.Id, token)).ReturnsAsync(player);
        _baseball
            .Setup(provider => provider.GetPlayerAsync(592450, token))
            .ReturnsAsync((ExternalPlayerProfile?)null);
        _baseball
            .Setup(provider => provider.GetSeasonHittingAsync(592450, 2026, token))
            .ReturnsAsync(hitting);
        _baseball
            .Setup(provider => provider.GetExpectedHittingAsync(592450, 2026, token))
            .ReturnsAsync((ExternalExpectedHitting?)null);
        _players
            .Setup(repository => repository.UpsertSeasonAsync(player, hitting, Now, token))
            .Returns((Player current, ExternalSeasonHitting season, DateTimeOffset updatedAt, CancellationToken _) =>
            {
                var stored = current.UpsertSeason(
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
                    updatedAt);
                return Task.FromResult(stored);
            });

        var profile = await _service.GetProfileAsync(player.Id, token);

        Assert.NotNull(profile.CurrentSeason);
        Assert.Equal(57, profile.CurrentSeason.Hits);
        Assert.Equal(18, profile.CurrentSeason.HomeRuns);
        Assert.Equal(0.241m, profile.CurrentSeason.BattingAverage);
        _players.Verify(repository => repository.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task GetProfile_WhenRefreshFails_ReturnsPersistedPlayerWithoutProviderText()
    {
        var player = SamplePlayer();
        player.UpsertSeason(2026, 10, 40, 36, 9, 1, 0, 2, 4, 8, 0, 0, Now.AddHours(-7));
        _players
            .Setup(repository => repository.GetByIdAsync(player.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);
        _baseball
            .Setup(provider => provider.GetPlayerAsync(592450, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalProviderException(
                "MlbStatsApi",
                "GetPlayer",
                "The baseball data provider could not be reached.",
                new HttpRequestException("statsapi.mlb.com refused the connection")));

        var profile = await _service.GetProfileAsync(player.Id, CancellationToken.None);

        Assert.Equal(9, profile.CurrentSeason!.Hits);
        Assert.Contains("refresh", profile.CurrentSeason.Freshness, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("statsapi", profile.CurrentSeason.Freshness, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpRequestException", profile.DataClassification, StringComparison.Ordinal);
        _players.Verify(
            repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Player SamplePlayer() =>
        Player.Create(
            592450,
            "Aaron",
            "Judge",
            "RF",
            BattingSide.Right,
            ThrowingArm.Right,
            new DateOnly(1992, 4, 26),
            Now);

    private sealed class FixedTime(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class MissCache : IMemoryCache
    {
        public void Dispose()
        {
        }

        public bool TryGetValue(object key, out object? value)
        {
            value = null;
            return false;
        }

        public ICacheEntry CreateEntry(object key) => throw new NotSupportedException();

        public void Remove(object key)
        {
        }
    }
}
