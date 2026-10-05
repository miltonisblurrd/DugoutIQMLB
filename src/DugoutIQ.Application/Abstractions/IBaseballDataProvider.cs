using DugoutIQ.Application.Baseball;

namespace DugoutIQ.Application.Abstractions;

public interface IBaseballDataProvider
{
    Task<IReadOnlyList<ExternalPlayer>> SearchPlayersAsync(
        string query,
        CancellationToken cancellationToken);

    Task<ExternalPlayerProfile?> GetPlayerAsync(
        int externalId,
        CancellationToken cancellationToken);

    Task<ExternalSeasonHitting?> GetSeasonHittingAsync(
        int externalId,
        int season,
        CancellationToken cancellationToken);

    Task<ExternalExpectedHitting?> GetExpectedHittingAsync(
        int externalId,
        int season,
        CancellationToken cancellationToken);
}
