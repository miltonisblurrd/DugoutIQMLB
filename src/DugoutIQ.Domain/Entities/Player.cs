using DugoutIQ.Domain.Enums;
using DugoutIQ.Domain.Exceptions;

namespace DugoutIQ.Domain.Entities;

public sealed class Player
{
    private readonly List<PlayerSeason> _seasons = [];

    public Guid Id { get; private set; }

    public int ExternalId { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string PositionAbbreviation { get; private set; } = null!;

    public BattingSide Bats { get; private set; }

    public ThrowingArm Throws { get; private set; }

    public DateOnly? BirthDate { get; private set; }

    public Guid? TeamId { get; private set; }

    public Team? Team { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<PlayerSeason> Seasons => _seasons;

    private Player()
    {
    }

    public static Player Create(
        int externalId,
        string firstName,
        string lastName,
        string positionAbbreviation,
        BattingSide bats,
        ThrowingArm throwsArm,
        DateOnly? birthDate,
        DateTimeOffset utcNow)
    {
        if (externalId <= 0)
        {
            throw new DomainException("A player external id must be positive.");
        }

        var player = new Player
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            CreatedAt = utcNow
        };

        player.ApplyProfile(firstName, lastName, positionAbbreviation, bats, throwsArm, birthDate, utcNow);
        return player;
    }

    public void ApplyProfile(
        string firstName,
        string lastName,
        string positionAbbreviation,
        BattingSide bats,
        ThrowingArm throwsArm,
        DateOnly? birthDate,
        DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(positionAbbreviation);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PositionAbbreviation = positionAbbreviation.Trim().ToUpperInvariant();
        Bats = bats;
        Throws = throwsArm;
        BirthDate = birthDate;
        UpdatedAt = utcNow;
    }

    public void AssignTeam(Team team, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(team);
        Team = team;
        TeamId = team.Id;
        UpdatedAt = utcNow;
    }

    public PlayerSeason UpsertSeason(
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
        DateTimeOffset utcNow)
    {
        var existing = _seasons.FirstOrDefault(row => row.Season == season);
        if (existing is null)
        {
            var created = PlayerSeason.Create(
                Id,
                season,
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
                utcNow);
            _seasons.Add(created);
            return created;
        }

        existing.ReplaceCountingStats(
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
            utcNow);
        UpdatedAt = utcNow;
        return existing;
    }
}
