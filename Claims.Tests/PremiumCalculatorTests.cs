using Claims.Premium;
using Xunit;

namespace Claims.Tests;

public class PremiumCalculatorTests
{
    private readonly PremiumCalculator _calculator=new();

    [Theory]
    [InlineData(CoverType.Yacht, 10, 13750)]
    [InlineData(CoverType.Tanker, 10, 18750)]
    [InlineData(CoverType.Yacht, 30, 41250)]
    [InlineData(CoverType.ContainerShip, 30, 48750)]
    [InlineData(CoverType.Yacht, 31, 42556.25)]
    [InlineData(CoverType.Yacht, 180, 237187.5)]
    [InlineData(CoverType.Yacht, 181, 238452.5)]
    [InlineData(CoverType.Yacht, 365, 471212.5)]
    [InlineData(CoverType.PassengerShip, 365, 534675)]
    [InlineData(CoverType.Tanker, 365, 668343.75)]
    [InlineData(CoverType.ContainerShip, 365, 579231.25)]
    [InlineData(CoverType.Yacht, 0, 0)]

    // Test method for calculating premiums based on cover type and duration 
    //Limitation of using decimals and dates, so we use double for expected values, and numbers of days that are added to a fixed start date
    public void Calculate_ReturnsExpectedPremium(CoverType type, int days, double expected)
    {
        // Arrange
        var start = new DateTime(2027, 1, 1);
        var end = start.AddDays(days);

        // Act
        var premium = _calculator.Calculate(start, end, type);

        // Assert
        Assert.Equal((decimal)expected, premium);
    }
}