using DugoutIQ.Application.Abstractions;
using DugoutIQ.Application.Baseball;
using DugoutIQ.Domain.Entities;
using DugoutIQ.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DugoutIQ.Infrastructure.Repositories;

public sealed class PlayerRepository : IPlayerRepository
{
    private readonly DugoutIQDbContext _db;

    public PlayerRepository(DugoutIQDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Player>> SearchByNameAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        // AsNoTracking: this query only displays players. EF does not need to
        // watch them for updates, so it skips the change-tracker snapshot.
        return await _db.Players
            .AsNoTracking()
            .Include(player => player.Team)
            .Where(player =>
                (player.FirstName + " " + player.LastName).Contains(query)
                || player.LastName.Contains(query))
            .OrderBy(player => player.LastName)
            .ThenBy(player => player.FirstName)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Players
            .Include(player => player.Team)
            .Include(player => player.Seasons)
            .FirstOrDefaultAsync(player => player.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Player>> UpsertPlayersAsync(
        IReadOnlyList<ExternalPlayer> players,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (players.Count == 0)
        {
            return [];
        }

        var externalIds = players.Select(player => player.ExternalId).Distinct().ToArray();
        var existing = await _db.Players
            .Where(player => externalIds.Contains(player.ExternalId))
            .ToListAsync(cancellationToken);

        var byExternalId = existing.ToDictionary(player => player.ExternalId);
        var results = new List<Player>(players.Count);

        foreach (var incoming in players)
        {
            if (!byExternalId.TryGetValue(incoming.ExternalId, out var player))
            {
                player = Player.Create(
                    incoming.ExternalId,
                    incoming.FirstName,
                    incoming.LastName,
                    incoming.PositionAbbreviation,
                    incoming.Bats,
                    incoming.Throws,
                    incoming.BirthDate,
                    utcNow);
                _db.Players.Add(player);
                byExternalId.Add(incoming.ExternalId, player);
            }
            else
            {
                player.ApplyProfile(
                    incoming.FirstName,
                    incoming.LastName,
                    incoming.PositionAbbreviation,
                    incoming.Bats,
                    incoming.Throws,
                    incoming.BirthDate,
                    utcNow);
            }

            results.Add(player);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return results;
    }

    public async Task<Team> UpsertTeamAsync(
        ExternalTeam incoming,
        CancellationToken cancellationToken)
    {
        var team = await _db.Teams
            .FirstOrDefaultAsync(row => row.ExternalId == incoming.ExternalId, cancellationToken);

        if (team is null)
        {
            team = Team.Create(
                incoming.ExternalId,
                incoming.Name,
                incoming.Abbreviation,
                incoming.League,
                incoming.Division);
            _db.Teams.Add(team);
            return team;
        }

        team.ApplyProfile(incoming.Name, incoming.Abbreviation, incoming.League, incoming.Division);
        return team;
    }

    public Task<PlayerSeason> UpsertSeasonAsync(
        Player player,
        ExternalSeasonHitting hitting,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var season = player.UpsertSeason(
            hitting.Season,
            hitting.Games,
            hitting.PlateAppearances,
            hitting.AtBats,
            hitting.Hits,
            hitting.Doubles,
            hitting.Triples,
            hitting.HomeRuns,
            hitting.Walks,
            hitting.Strikeouts,
            hitting.HitByPitch,
            hitting.SacrificeFlies,
            utcNow);

        // A season created in memory is not tracked until we hand it to the context.
        // A season loaded with the player is already tracked; adding it again would throw.
        if (_db.Entry(season).State == EntityState.Detached)
        {
            _db.PlayerSeasons.Add(season);
        }

        return Task.FromResult(season);
    }

    public Task<StatcastSnapshot?> GetLatestSnapshotAsync(Guid playerId, CancellationToken cancellationToken) =>
        _db.StatcastSnapshots
            .Where(snapshot => snapshot.PlayerId == playerId)
            .OrderByDescending(snapshot => snapshot.CapturedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public void AddSnapshot(StatcastSnapshot snapshot) => _db.StatcastSnapshots.Add(snapshot);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _db.SaveChangesAsync(cancellationToken);
}
