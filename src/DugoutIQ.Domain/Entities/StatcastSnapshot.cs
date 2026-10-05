using DugoutIQ.Domain.Exceptions;

namespace DugoutIQ.Domain.Entities;

/// <summary>
/// One observation of a player's underlying metrics at a point in time.
/// Null columns mean the provider did not supply that metric. They are not zeros.
/// </summary>
public sealed class StatcastSnapshot
{
    public Guid Id { get; private set; }

    public Guid PlayerId { get; private set; }

    public Player Player { get; private set; } = null!;

    /// <summary>
    /// The season the observed rates describe. Expected statistics from the
    /// current provider are season aggregates, so the year has to travel with the row.
    /// </summary>
    public int Season { get; private set; }

    public DateTimeOffset CapturedAt { get; private set; }

    public decimal? AverageExitVelocity { get; private set; }

    public decimal? MaxExitVelocity { get; private set; }

    public decimal? BarrelPercentage { get; private set; }

    public decimal? HardHitPercentage { get; private set; }

    public decimal? LaunchAngle { get; private set; }

    public decimal? ExpectedBattingAverage { get; private set; }

    public decimal? ExpectedSlugging { get; private set; }

    public decimal? ExpectedWoba { get; private set; }

    public decimal? WhiffPercentage { get; private set; }

    public decimal? ChasePercentage { get; private set; }

    private StatcastSnapshot()
    {
    }

    public static StatcastSnapshot CaptureExpectedHitting(
        Guid playerId,
        int season,
        decimal? expectedBattingAverage,
        decimal? expectedSlugging,
        decimal? expectedWoba,
        DateTimeOffset capturedAt)
    {
        if (playerId == Guid.Empty)
        {
            throw new DomainException("A snapshot must belong to a player.");
        }

        if (expectedBattingAverage is null && expectedSlugging is null && expectedWoba is null)
        {
            throw new DomainException("A snapshot needs at least one observed metric.");
        }

        return new StatcastSnapshot
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            Season = season,
            CapturedAt = capturedAt,
            ExpectedBattingAverage = expectedBattingAverage,
            ExpectedSlugging = expectedSlugging,
            ExpectedWoba = expectedWoba
        };
    }
}
