using BonusTest.Application.Common;
using BonusTest.Domain.Constants;

namespace BonusTest.Application.Bonuses;

public static class BonusCalculator
{
    public static BonusCalculationResult Calculate(
        int bonusTypeId,
        decimal value,
        decimal monthlySalary,
        decimal taxRate)
    {
        var gross = bonusTypeId switch
        {
            BonusTypes.FixedAmount => RoundMoney(value),
            BonusTypes.SalaryPercentage => RoundMoney(monthlySalary * value / 100m),
            _ => throw new ArgumentOutOfRangeException(nameof(bonusTypeId))
        };

        var tax = RoundMoney(gross * taxRate / 100m);
        var net = RoundMoney(gross - tax);
        return new BonusCalculationResult(gross, tax, net);
    }

    public static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
}
