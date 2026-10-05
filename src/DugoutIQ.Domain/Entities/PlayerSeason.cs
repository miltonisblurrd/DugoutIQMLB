using DugoutIQ.Domain.Exceptions;
using DugoutIQ.Domain.Rules;

namespace DugoutIQ.Domain.Entities;

/// <summary>
/// Regular-season counting stats for one player and year.
/// Rate stats are calculated, not stored, so a formula change does not
/// require rewriting historical rows.
/// </summary>
public sealed class PlayerSeason
{
    public Guid Id { get; private set; }

    public Guid PlayerId { get; private set; }

    public Player Player { get; private set; } = null!;

    public int Season { get; private set; }

    public int Games { get; private set; }

    public int PlateAppearances { get; private set; }

    public int AtBats { get; private set; }

    public int Hits { get; private set; }

    public int Doubles { get; private set; }

    public int Triples { get; private set; }

    public int HomeRuns { get; private set; }

    public int Walks { get; private set; }

    public int Strikeouts { get; private set; }

    /// <summary>
    /// Required to calculate on-base percentage. Walks alone are not enough.
    /// </summary>
    public int HitByPitch { get; private set; }

    /// <summary>
    /// Required to calculate on-base percentage.
    /// </summary>
    public int SacrificeFlies { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public BattingRateLine Rates => BattingRateLine.Calculate(
        AtBats,
        Hits,
        Doubles,
        Triples,
        HomeRuns,
        Walks,
        HitByPitch,
        SacrificeFlies);

    private PlayerSeason()
    {
    }

    public static PlayerSeason Create(
        Guid playerId,
        int season,
        int games,
        int plateAppearances,
        int atBats,
        int hits,
        int doubles,
        int triples,
        int homeRuns,
        int walks,
        int strikeouts,
        int hitByPitch,
        int sacrificeFlies,
        DateTimeOffset updatedAt)
    {
        if (playerId == Guid.Empty)
        {
            throw new DomainException("A season must belong to a player.");
        }

        if (season < 1876 || season > 2100)
        {
            throw new DomainException($"Season {season} is outside the supported range.");
        }

        var seasonRow = new PlayerSeason
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Season = season,
            UpdatedAt = updatedAt
        };

        seasonRow.ReplaceCountingStats(
            games,
            plateAppearances,
            atBats,
            hits,
            doubles,
            triples,
            homeRuns,
            walks,
            strikeouts,
            hitByPitch,
            sacrificeFlies,
            updatedAt);

        return seasonRow;
    }

    public void ReplaceCountingStats(
        int games,
        int plateAppearances,
        int atBats,
        int hits,
        int doubles,
        int triples,
        int homeRuns,
        int walks,
        int strikeouts,
        int hitByPitch,
        int sacrificeFlies,
        DateTimeOffset updatedAt)
    {
        // Calculating the line validates the counting stats before we store them.
        _ = BattingRateLine.Calculate(
            atBats,
            hits,
            doubles,
            triples,
            homeRuns,
            walks,
            hitByPitch,
            sacrificeFlies);

        if (games < 0 || plateAppearances < 0 || strikeouts < 0)
        {
            throw new DomainException("Counting stats cannot be negative.");
        }

        Games = games;
        PlateAppearances = plateAppearances;
        AtBats = atBats;
        Hits = hits;
        Doubles = doubles;
        Triples = triples;
        HomeRuns = homeRuns;
        Walks = walks;
        Strikeouts = strikeouts;
        HitByPitch = hitByPitch;
        SacrificeFlies = sacrificeFlies;
        UpdatedAt = updatedAt;
    }
}
