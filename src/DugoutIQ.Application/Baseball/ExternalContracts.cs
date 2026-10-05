using DugoutIQ.Domain.Enums;

namespace DugoutIQ.Application.Baseball;

/// <summary>
/// Provider-neutral player identity. MLB JSON never leaves Infrastructure.
/// </summary>
public sealed record ExternalPlayer(
    int ExternalId,
    string FirstName,
    string LastName,
    string PositionAbbreviation,
    BattingSide Bats,
    ThrowingArm Throws,
    DateOnly? BirthDate);

public sealed record ExternalTeam(
    int ExternalId,
    string Name,
    string Abbreviation,
    League League,
    string Division);

public sealed record ExternalPlayerProfile(
    ExternalPlayer Player,
    ExternalTeam? CurrentTeam);

public sealed record ExternalSeasonHitting(
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
    int SacrificeFlies);

public sealed record ExternalExpectedHitting(
    int Season,
    decimal? ExpectedBattingAverage,
    decimal? ExpectedSlugging,
    decimal? ExpectedWoba);
