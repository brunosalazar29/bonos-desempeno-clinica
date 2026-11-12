using BonusTest.Application.Bonuses;
using BonusTest.Domain.Constants;
using Xunit;

namespace BonusTest.Tests;

public sealed class BonusCalculatorTests
{
    [Fact]
    public void Calculate_FixedAmount_ComputesGrossTaxAndNet()
    {
        var result = BonusCalculator.Calculate(
            BonusTypes.FixedAmount,
            250m,
            5000m,
            8m);

        Assert.Equal(250.00m, result.GrossAmount);
        Assert.Equal(20.00m, result.TaxAmount);
        Assert.Equal(230.00m, result.NetAmount);
    }

    [Fact]
    public void Calculate_SalaryPercentage_ComputesExpectedAmounts()
    {
        var result = BonusCalculator.Calculate(
            BonusTypes.SalaryPercentage,
            10m,
            5000m,
            8m);

        Assert.Equal(500.00m, result.GrossAmount);
        Assert.Equal(40.00m, result.TaxAmount);
        Assert.Equal(460.00m, result.NetAmount);
    }

    [Fact]
    public void Calculate_RoundsMidpointsAwayFromZero()
    {
        var result = BonusCalculator.Calculate(
            BonusTypes.FixedAmount,
            0.125m,
            1000m,
            50m);

        Assert.Equal(0.13m, result.GrossAmount);
        Assert.Equal(0.07m, result.TaxAmount);
        Assert.Equal(0.06m, result.NetAmount);
    }

    [Fact]
    public void Calculate_UnknownBonusType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BonusCalculator.Calculate(999, 100m, 5000m, 8m));
    }
}
