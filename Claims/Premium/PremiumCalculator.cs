namespace Claims.Premium;

public class PremiumCalculator : IPremiumCalculator
{

    private const decimal BaseDayRate = 1250m;
    public decimal Calculate(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        var multiplier = GetMultiplier(coverType);

        var dailyRate = BaseDayRate * multiplier;
        var days = (endDate - startDate).Days;
        var totalPremium = 0m;

        for (var day = 0; day < days; day++)
        {
            totalPremium += dailyRate * (1 - GetDiscount(day, coverType));
        }

        return totalPremium;
    }
    private static decimal GetMultiplier(CoverType coverType) => coverType switch
    {
        CoverType.Yacht => 1.1m,
        CoverType.PassengerShip => 1.2m,
        CoverType.Tanker => 1.5m,
        _ => 1.3m
    };

    private static decimal GetDiscount(int day, CoverType coverType)
    {
        var isYacht = coverType == CoverType.Yacht;

        if (day < 30)
        {
            return 0m;
        }

        if (day < 180)
        {
            return isYacht ? 0.05m : 0.02m;
        }

        return isYacht ? 0.08m : 0.03m;
    }
}