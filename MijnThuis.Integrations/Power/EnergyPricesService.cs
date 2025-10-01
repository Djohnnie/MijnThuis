using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Xml.Serialization;

namespace MijnThuis.Integrations.Power;

public class EnergyPricesForDate
{
    public DateTime Date { get; set; }
    public List<EnergyPrice> Prices { get; set; } = new List<EnergyPrice>();
}

public class EnergyPrice
{
    public DateTime TimeStamp { get; set; }
    public decimal Price { get; set; }
}

public interface IEnergyPricesService
{
    Task<EnergyPricesForDate> GetEnergyPricesForDate(DateTime date);
}

public class EnergyPricesService : EnergyPricesBaseService, IEnergyPricesService
{
    private readonly string _apiKey;
    private readonly ILogger<EnergyPricesService> _logger;

    public EnergyPricesService(
        IConfiguration configuration,
        ILogger<EnergyPricesService> logger) : base(configuration)
    {
        _apiKey = configuration.GetValue<string>("ENERGY_PRICES_API_KEY");
        _logger = logger;
    }

    public async Task<EnergyPricesForDate> GetEnergyPricesForDate(DateTime date)
    {
        var client = InitializeHttpClient();

        var domain = "10YBE----------2";
        var start = $"{date:yyyyMMdd}0000";
        var end = $"{date:yyyyMMdd}2359";

        var url = $"?documentType=A44&in_Domain={domain}&out_Domain={domain}&periodStart={start}&periodEnd={end}&securityToken={_apiKey}";
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error fetching data from API: {response.StatusCode}");
        }

        var stream = await response.Content.ReadAsStreamAsync();

        var serializer = new XmlSerializer(typeof(PublicationMarketDocument));
        var result = serializer.Deserialize(stream) as PublicationMarketDocument;

        // From 2025-10-01, tarrifs will be for each 15 minutes instead of each hour.
        var factor = date >= new DateTime(2025, 10, 1) ? 4 : 1;

        var period = result.TimeSeries.First().Periods.First();
        period.Points.ForEach(x => x.Position = period.Resolution == "PT15M" && factor == 1 ? (x.Position - 1) / 4 : x.Position - 1);

        var sortedPoints = period.Points.OrderBy(x => x.Position).ToList();

        var periodsInDay = GetPeriodsInDay(date, factor);
        if (sortedPoints.Count != periodsInDay)
        {
            if (sortedPoints.Count < periodsInDay)
            {
                for (var periodInDay = 0; periodInDay < periodsInDay; periodInDay++)
                {
                    if (periodInDay >= sortedPoints.Count)
                    {
                        sortedPoints.Add(new Point
                        {
                            Position = periodInDay,
                            Price = sortedPoints[periodInDay - 1].Price
                        });
                    }
                    else
                    {
                        var point = sortedPoints[periodInDay];
                        if (point.Position > periodInDay)
                        {
                            sortedPoints.Insert(periodInDay, new Point
                            {
                                Position = periodInDay,
                                Price = sortedPoints[periodInDay - 1].Price
                            });
                        }
                    }
                }
            }
            else
            {
                throw new Exception($"{date} should have {periodsInDay} periods, but service returned {sortedPoints.Count} points.");
            }
        }

        var energyPrices = new List<EnergyPrice>();
        for (int periodInDay = 0; periodInDay < periodsInDay; periodInDay++)
        {
            var point = sortedPoints[periodInDay];

            var periodOffset = 0;

            if (periodsInDay == 23 * factor)
            {
                periodOffset = periodInDay < 2 ? periodInDay : periodInDay + 1;
            }
            else if (periodsInDay == 24 * factor)
            {
                periodOffset = periodInDay;
            }
            else if (periodsInDay == 25 * factor)
            {
                periodOffset = periodInDay < 3 ? periodInDay : periodInDay - 1;
            }
            else
            {
                throw new Exception($"Invalid number of periods in day: {periodsInDay}");
            }

            energyPrices.Add(new EnergyPrice
            {
                TimeStamp = date.AddMinutes(periodOffset * (factor == 1 ? 60 : 15)),
                Price = point.Price
            });
        }

        return new EnergyPricesForDate
        {
            Date = date,
            Prices = energyPrices
        };
    }

    private int GetPeriodsInDay(DateTime date, int factor)
    {
        var adjustmentRules = TimeZoneInfo.Local.GetAdjustmentRules();

        // There should only be one adjustment rule for the current date.
        var applicableAdjustmentRules = adjustmentRules.Where(x => x.DateStart <= date && x.DateEnd >= date);

        foreach (var rule in applicableAdjustmentRules)
        {
            _logger.LogInformation($"Adjustment rule for {date}: {rule.DateStart} - {rule.DateEnd}");
        }

        var currentAdjustmentRule = applicableAdjustmentRules.SingleOrDefault();

        if (currentAdjustmentRule == null)
        {
            // No adjustment rule found, return the default 24 hours.
            return 24 * factor;
        }

        var start = GetTransitionDate(currentAdjustmentRule.DaylightTransitionStart, date.Year);
        if (date == start)
        {
            // This day has 23 hours, and should skip an hour.
            return 23 * factor;
        }

        var end = GetTransitionDate(currentAdjustmentRule.DaylightTransitionEnd, date.Year);
        if (date == end)
        {
            // This day has 25 hours, and should add an extra hour.
            return 25 * factor;
        }

        // Return default of 24 hours.
        return 24 * factor;
    }

    private static DateTime GetTransitionDate(TimeZoneInfo.TransitionTime transitionTime, int year)
    {
        if (transitionTime.IsFixedDateRule)
        {
            return new DateTime(year, transitionTime.Month, transitionTime.Day);
        }
        else
        {
            if (transitionTime.Week == 5)
            {
                // Special value meaning the last DayOfWeek (e.g., Sunday) in the month.
                DateTime transitionDate = new DateTime(year, transitionTime.Month, 1);
                transitionDate = transitionDate.AddMonths(1);

                transitionDate = transitionDate.AddDays(-1);
                while (transitionDate.DayOfWeek != transitionTime.DayOfWeek)
                {
                    transitionDate = transitionDate.AddDays(-1);
                }

                return transitionDate;
            }
            else
            {
                DateTime transitionDate = new DateTime(year, transitionTime.Month, 1);
                transitionDate = transitionDate.AddDays(-1);

                for (int howManyWeeks = 0; howManyWeeks < transitionTime.Week; howManyWeeks++)
                {
                    transitionDate = transitionDate.AddDays(1);
                    while (transitionDate.DayOfWeek != transitionTime.DayOfWeek)
                    {
                        transitionDate = transitionDate.AddDays(1);
                    }
                }

                return transitionDate;
            }
        }
    }
}

public class EnergyPricesBaseService
{
    private readonly string _baseAddress;

    protected EnergyPricesBaseService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("ENERGY_PRICES_API_BASE_ADDRESS");
    }

    protected HttpClient InitializeHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseAddress);

        return client;
    }
}

[XmlRoot("Publication_MarketDocument", Namespace = "urn:iec62325.351:tc57wg16:451-3:publicationdocument:7:3")]
public class PublicationMarketDocument
{
    [XmlElement("revisionNumber")]
    public int RevisionNumber { get; set; }

    [XmlElement("TimeSeries")]
    public List<TimeSeries> TimeSeries { get; set; }
}

public class TimeSeries
{
    [XmlElement("Period")]
    public List<Period> Periods { get; set; }
}

public class Period
{
    [XmlElement("resolution")]
    public string Resolution { get; set; }

    [XmlElement("Point")]
    public List<Point> Points { get; set; }
}

public class Point
{
    [XmlElement("position")]
    public int Position { get; set; }

    [XmlElement("price.amount")]
    public decimal Price { get; set; }
}