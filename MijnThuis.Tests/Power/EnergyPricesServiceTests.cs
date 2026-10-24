using MijnThuis.Integrations.Power;
using Shouldly;
using Xunit;

namespace MijnThuis.Tests.Power;

public class EnergyPricesServiceTests
{
    [Theory]
    [InlineData(2026, 10, 25, 4, 25)]
    [InlineData(2026, 3, 29, 4, 23)]
    [InlineData(2026, 10, 24, 4, 24)]
    [InlineData(2024, 10, 27, 1, 25)]
    [InlineData(2024, 3, 31, 1, 23)]
    [InlineData(2024, 10, 26, 1, 24)]
    public void MapsPeriodsToBelgianTimeKeepingTheLastPriceForRepeatedTimes(int year, int month, int day, int factor, int hours)
    {
        var date = new DateTime(year, month, day);
        var minutes = 60 / factor;
        var count = hours * factor;
        var period = new Period
        {
            Resolution = factor == 4 ? "PT15M" : "PT60M",
            Points = Enumerable.Range(1, count)
                .Select(position => new Point { Position = position, Price = position })
                .ToList()
        };
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels");
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(date, timeZone);
        var expectedPrices = new Dictionary<DateTime, decimal>();
        for (var index = 0; index < count; index++)
        {
            var timeStamp = TimeZoneInfo.ConvertTimeFromUtc(startUtc.AddMinutes(index * minutes), timeZone);
            expectedPrices[timeStamp] = index + 1;
        }

        var prices = EnergyPricesService.MapEnergyPrices(date, period, factor, count);

        prices.Count.ShouldBe(Math.Min(hours, 24) * factor);
        prices.Select(price => price.TimeStamp).ShouldBe(expectedPrices.Keys.OrderBy(timeStamp => timeStamp));
        foreach (var price in prices)
        {
            price.Price.ShouldBe(expectedPrices[price.TimeStamp]);
        }
        prices.All(price => price.TimeStamp.Date == date).ShouldBeTrue();
        prices[^1].TimeStamp.ShouldBe(date.AddDays(1).AddMinutes(-minutes));
    }

    [Fact]
    public void AutumnQuarterHourlyPricesKeepDistinctEarlySlotsAndFillUnchangedPrices()
    {
        var date = new DateTime(2026, 10, 25);
        var period = new Period
        {
            Resolution = "PT15M",
            Points = Enumerable.Range(1, 100)
                .Where(position => position is not (10 or 11 or 12 or 16 or 44 or 45))
                .Select(position => new Point
                {
                    Position = position,
                    Price = position switch
                    {
                        3 => 8.16M,
                        4 => 7.01M,
                        9 or 15 or 43 => 0M,
                        13 => 3.88M,
                        100 => 21.03M,
                        _ => position
                    }
                })
                .ToList()
        };

        var prices = EnergyPricesService.MapEnergyPrices(date, period, 4, 100);

        prices.Count.ShouldBe(96);
        prices.Single(price => price.TimeStamp == date.AddMinutes(30)).Price.ShouldBe(8.16M);
        prices.Single(price => price.TimeStamp == date.AddMinutes(45)).Price.ShouldBe(7.01M);
        foreach (var index in new[] { 10, 11, 38, 39, 40 })
        {
            prices[index].Price.ShouldBe(0M);
        }
        prices[8].TimeStamp.ShouldBe(date.AddHours(2));
        prices[8].Price.ShouldBe(3.88M);
        prices[9].TimeStamp.ShouldBe(date.AddHours(2).AddMinutes(15));
        prices[9].Price.ShouldBe(14M);
        prices.Select(price => price.TimeStamp).Distinct().Count().ShouldBe(96);
        prices[^1].TimeStamp.ShouldBe(date.AddHours(23).AddMinutes(45));
        prices[^1].Price.ShouldBe(21.03M);
    }
}
