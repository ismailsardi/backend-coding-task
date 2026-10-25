namespace Claims.Premium;

public interface IPremiumCalculator
{
    decimal Calculate(DateTime startDate, DateTime endDate, CoverType coverType);
}

    