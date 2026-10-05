using DugoutIQ.Application.Baseball;
using DugoutIQ.Domain.Entities;

namespace DugoutIQ.Application.Abstractions;

public interface IPlayerRepository
{
    Task<IReadOnlyList<Player>> SearchByNameAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);

    Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Player>> UpsertPlayersAsync(
        IReadOnlyList<ExternalPlayer> players,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<Team> UpsertTeamAsync(ExternalTeam team, CancellationToken cancellationToken);

    Task<PlayerSeason> UpsertSeasonAsync(
        Player player,
        ExternalSeasonHitting hitting,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<StatcastSnapshot?> GetLatestSnapshotAsync(Guid playerId, CancellationToken cancellationToken);

    void AddSnapshot(StatcastSnapshot snapshot);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
