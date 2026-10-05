using DugoutIQ.Domain.Exceptions;
using DugoutIQ.Domain.Rules;

namespace DugoutIQ.UnitTests;

public class BattingRateLineTests
{
    [Fact]
    public void Calculate_NormalLine_MatchesPublishedRounding()
    {
        // Aaron Judge, 2025 regular season, calculated independently:
        // AVG = 179 / 541 = 0.33086... -> .331
        // TB  = 179 + 30 + (2 * 2) + (3 * 53) = 372
        // SLG = 372 / 541 = 0.68761... -> .688
        // OBP = (179 + 124 + 7) / (541 + 124 + 7 + 7) = 310 / 679 = 0.45655... -> .457
        // OPS = .457 + .688 = 1.145
        var line = BattingRateLine.Calculate(
            atBats: 541,
            hits: 179,
            doubles: 30,
            triples: 2,
            homeRuns: 53,
            walks: 124,
            hitByPitch: 7,
            sacrificeFlies: 7);

        Assert.Equal(0.331m, line.BattingAverage);
        Assert.Equal(0.457m, line.OnBasePercentage);
        Assert.Equal(0.688m, line.SluggingPercentage);
        Assert.Equal(1.145m, line.Ops);
    }

    [Fact]
    public void Calculate_Single_CountsOneTotalBase()
    {
        // 1 for 1, no extra-base hits. TB = 1. SLG = 1.000. OBP = 1/1.
        var line = BattingRateLine.Calculate(1, 1, 0, 0, 0, 0, 0, 0);

        Assert.Equal(1.000m, line.BattingAverage);
        Assert.Equal(1.000m, line.OnBasePercentage);
        Assert.Equal(1.000m, line.SluggingPercentage);
        Assert.Equal(2.000m, line.Ops);
    }

    [Fact]
    public void Calculate_Double_CountsTwoTotalBases()
    {
        var line = BattingRateLine.Calculate(1, 1, 1, 0, 0, 0, 0, 0);

        Assert.Equal(1.000m, line.BattingAverage);
        Assert.Equal(2.000m, line.SluggingPercentage);
    }

    [Fact]
    public void Calculate_Triple_CountsThreeTotalBases()
    {
        var line = BattingRateLine.Calculate(1, 1, 0, 1, 0, 0, 0, 0);

        Assert.Equal(3.000m, line.SluggingPercentage);
    }

    [Fact]
    public void Calculate_HomeRun_CountsFourTotalBases()
    {
        var line = BattingRateLine.Calculate(1, 1, 0, 0, 1, 0, 0, 0);

        Assert.Equal(4.000m, line.SluggingPercentage);
        Assert.Equal(1.000m, line.BattingAverage);
    }

    [Fact]
    public void Calculate_Walks_RaiseOnBasePercentageWithoutChangingAverage()
    {
        // AVG = 0 / 10 = .000
        // OBP = (0 + 5 + 0) / (10 + 5 + 0 + 0) = 5 / 15 = .333
        // SLG = 0 / 10 = .000
        var line = BattingRateLine.Calculate(10, 0, 0, 0, 0, 5, 0, 0);

        Assert.Equal(0.000m, line.BattingAverage);
        Assert.Equal(0.333m, line.OnBasePercentage);
        Assert.Equal(0.000m, line.SluggingPercentage);
        Assert.Equal(0.333m, line.Ops);
    }

    [Fact]
    public void Calculate_HitByPitch_IsATimeOnBase()
    {
        // OBP = (2 + 0 + 1) / (10 + 0 + 1 + 0) = 3 / 11 = 0.27272... -> .273
        // AVG = 2 / 10 = .200
        var line = BattingRateLine.Calculate(10, 2, 0, 0, 0, 0, 1, 0);

        Assert.Equal(0.200m, line.BattingAverage);
        Assert.Equal(0.273m, line.OnBasePercentage);
        Assert.Equal(0.200m, line.SluggingPercentage);
        Assert.Equal(0.473m, line.Ops);
    }

    [Fact]
    public void Calculate_SacrificeFlies_IncreaseTheOnBaseDenominatorOnly()
    {
        // With the two sacrifice flies:
        // OBP = (3 + 1 + 0) / (10 + 1 + 0 + 2) = 4 / 13 = 0.30769... -> .308
        // Leaving the flies out of the denominator would be 4 / 11 = .364.
        var line = BattingRateLine.Calculate(10, 3, 0, 0, 0, 1, 0, 2);

        Assert.Equal(0.300m, line.BattingAverage);
        Assert.Equal(0.308m, line.OnBasePercentage);
        Assert.Equal(0.300m, line.SluggingPercentage);
        Assert.Equal(0.608m, line.Ops);
    }

    [Fact]
    public void Calculate_ZeroAtBats_LeavesAverageAndSluggingUndefined()
    {
        var line = BattingRateLine.Calculate(0, 0, 0, 0, 0, 0, 0, 0);

        Assert.Null(line.BattingAverage);
        Assert.Null(line.OnBasePercentage);
        Assert.Null(line.SluggingPercentage);
        Assert.Null(line.Ops);
    }

    [Fact]
    public void Calculate_WalksWithZeroAtBats_ProducesOnBasePercentageOnly()
    {
        // OBP = 2 / 2 = 1.000. There is no at-bat, so AVG, SLG, and OPS are undefined.
        var line = BattingRateLine.Calculate(0, 0, 0, 0, 0, 2, 0, 0);

        Assert.Null(line.BattingAverage);
        Assert.Null(line.SluggingPercentage);
        Assert.Null(line.Ops);
        Assert.Equal(1.000m, line.OnBasePercentage);
    }

    [Fact]
    public void Calculate_OnlyHitByPitch_ProducesOnBasePercentage()
    {
        var line = BattingRateLine.Calculate(0, 0, 0, 0, 0, 0, 1, 0);

        Assert.Equal(1.000m, line.OnBasePercentage);
        Assert.Null(line.BattingAverage);
        Assert.Null(line.Ops);
    }

    [Fact]
    public void Calculate_OnlySacrificeFly_ProducesAZeroOnBasePercentage()
    {
        // Numerator 0, denominator 1. The fly is not a time on base.
        var line = BattingRateLine.Calculate(0, 0, 0, 0, 0, 0, 0, 1);

        Assert.Equal(0.000m, line.OnBasePercentage);
        Assert.Null(line.BattingAverage);
        Assert.Null(line.SluggingPercentage);
        Assert.Null(line.Ops);
    }

    [Fact]
    public void Calculate_RepeatingDecimal_RoundsHalfUpAtThreePlaces()
    {
        // 2 / 3 = 0.6666... -> .667
        var line = BattingRateLine.Calculate(3, 2, 0, 0, 0, 0, 0, 0);

        Assert.Equal(0.667m, line.BattingAverage);
        Assert.Equal(0.667m, line.SluggingPercentage);
    }

    [Fact]
    public void Calculate_ExactHalfThousandth_RoundsAwayFromZero()
    {
        // 1 / 2000 = 0.0005 exactly.
        // Three-decimal baseball rounding takes that half away from zero, to .001.
        // Round-half-to-even would have stayed at .000 because the third digit is even.
        var line = BattingRateLine.Calculate(2000, 1, 0, 0, 0, 0, 0, 0);

        Assert.Equal(0.001m, line.BattingAverage);
        Assert.Equal(0.001m, line.OnBasePercentage);
        Assert.Equal(0.001m, line.SluggingPercentage);
        Assert.Equal(0.002m, line.Ops);
    }

    [Fact]
    public void Calculate_NegativeCountingStat_Throws()
    {
        var exception = Assert.Throws<DomainException>(() =>
            BattingRateLine.Calculate(10, -1, 0, 0, 0, 0, 0, 0));

        Assert.Contains("negative", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calculate_ExtraBaseHitsExceedHits_Throws()
    {
        Assert.Throws<DomainException>(() =>
            BattingRateLine.Calculate(10, 1, 0, 0, 2, 0, 0, 0));
    }
}
