using DugoutIQ.Domain.Exceptions;

namespace DugoutIQ.Domain.Rules;

/// <summary>
/// Rate stats derived from counting stats. Null means the rate is undefined,
/// which is different from a real zero. OPS is the sum of the already-rounded
/// OBP and SLG, matching how baseball publishes the number.
/// </summary>
public readonly record struct BattingRateLine(
    decimal? BattingAverage,
    decimal? OnBasePercentage,
    decimal? SluggingPercentage,
    decimal? Ops)
{
    public static BattingRateLine Calculate(
        int atBats,
        int hits,
        int doubles,
        int triples,
        int homeRuns,
        int walks,
        int hitByPitch,
        int sacrificeFlies)
    {
        if (atBats < 0 || hits < 0 || doubles < 0 || triples < 0 || homeRuns < 0
            || walks < 0 || hitByPitch < 0 || sacrificeFlies < 0)
        {
            throw new DomainException("Counting stats cannot be negative.");
        }

        if (hits < doubles + triples + homeRuns)
        {
            throw new DomainException("Hits cannot be lower than doubles, triples, and home runs combined.");
        }

        var battingAverage = atBats == 0
            ? (decimal?)null
            : RoundToThousandths((decimal)hits / atBats);

        var onBaseDenominator = atBats + walks + hitByPitch + sacrificeFlies;
        var onBasePercentage = onBaseDenominator == 0
            ? (decimal?)null
            : RoundToThousandths((decimal)(hits + walks + hitByPitch) / onBaseDenominator);

        // Total bases = singles + 2*doubles + 3*triples + 4*home runs
        // which simplifies to hits + doubles + 2*triples + 3*home runs.
        var totalBases = hits + doubles + (2 * triples) + (3 * homeRuns);
        var sluggingPercentage = atBats == 0
            ? (decimal?)null
            : RoundToThousandths((decimal)totalBases / atBats);

        var ops = onBasePercentage is null || sluggingPercentage is null
            ? (decimal?)null
            : onBasePercentage.Value + sluggingPercentage.Value;

        return new BattingRateLine(battingAverage, onBasePercentage, sluggingPercentage, ops);
    }

    private static decimal RoundToThousandths(decimal value) =>
        Math.Round(value, 3, MidpointRounding.AwayFromZero);
}
