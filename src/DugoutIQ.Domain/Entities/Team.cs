using DugoutIQ.Domain.Enums;
using DugoutIQ.Domain.Exceptions;

namespace DugoutIQ.Domain.Entities;

public sealed class Team
{
    private readonly List<Player> _players = [];

    public Guid Id { get; private set; }

    public int ExternalId { get; private set; }

    public string Name { get; private set; } = null!;

    public string Abbreviation { get; private set; } = null!;

    public League League { get; private set; }

    public string Division { get; private set; } = null!;

    public IReadOnlyCollection<Player> Players => _players;

    private Team()
    {
    }

    public static Team Create(int externalId, string name, string abbreviation, League league, string division)
    {
        if (externalId <= 0)
        {
            throw new DomainException("A team external id must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(abbreviation);
        ArgumentException.ThrowIfNullOrWhiteSpace(division);

        return new Team
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Name = name.Trim(),
            Abbreviation = abbreviation.Trim().ToUpperInvariant(),
            League = league,
            Division = division.Trim()
        };
    }

    public void ApplyProfile(string name, string abbreviation, League league, string division)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(abbreviation);
        ArgumentException.ThrowIfNullOrWhiteSpace(division);

        Name = name.Trim();
        Abbreviation = abbreviation.Trim().ToUpperInvariant();
        League = league;
        Division = division.Trim();
    }
}
