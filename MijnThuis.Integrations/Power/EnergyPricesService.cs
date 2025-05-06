using Microsoft.Extensions.Configuration;
using System;
using System.Xml.Serialization;
using static System.TimeZoneInfo;

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

    public EnergyPricesService(IConfiguration configuration) : base(configuration)
    {
        _apiKey = configuration.GetValue<string>("ENERGY_PRICES_API_KEY");
    }

    public async Task<EnergyPricesForDate> GetEnergyPricesForDate(DateTime date)
    {
        var client = InitializeHttpClient();

        var domain = "10YBE----------2";
        var start = $"{date:yyyyMMdd}0000";
        var end = $"{date:yyyyMMdd}2300";

        var url = $"?documentType=A44&in_Domain={domain}&out_Domain={domain}&periodStart={start}&periodEnd={end}&securityToken={_apiKey}";
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error fetching data from API: {response.StatusCode}");
        }

        var stream = await response.Content.ReadAsStreamAsync();

        var serializer = new XmlSerializer(typeof(PublicationMarketDocument));
        var result = serializer.Deserialize(stream) as PublicationMarketDocument;

        var period = result.TimeSeries.First().Periods.First();
        period.Points.ForEach(x => x.Position = period.Resolution == "PT15M" ? (x.Position - 1) / 4 : x.Position - 1);

        var sortedPoints = period.Points.OrderBy(x => x.Position).ToList();

        var hoursInDay = GetHoursInDay(date);
        if (sortedPoints.Count != hoursInDay)
        {
            if (sortedPoints.Count < hoursInDay)
            {
                for (var hour = 0; hour < hoursInDay; hour++)
                {
                    var point = sortedPoints[hour];
                    if (point.Position > hour)
                    {
                        sortedPoints.Insert(hour, new Point
                        {
                            Position = hour,
                            Price = sortedPoints[hour - 1].Price
                        });
                    }
                }
            }
            else
            {
                throw new Exception($"{date} should have {hoursInDay} hours, but service returned {sortedPoints.Count} points.");
            }
        }

        var energyPrices = new List<EnergyPrice>();
        for (int hour = 0; hour < hoursInDay; hour++)
        {
            var point = sortedPoints[hour];

            var hourOffset = hoursInDay switch
            {
                23 => hour < 2 ? hour : hour + 1,
                24 => hour,
                25 => hour < 3 ? hour : hour - 1,
                _ => throw new Exception($"Invalid number of hours in day: {hoursInDay}")
            };

            energyPrices.Add(new EnergyPrice
            {
                TimeStamp = date.AddHours(hourOffset),
                Price = point.Price
            });
        }

        return new EnergyPricesForDate
        {
            Date = date,
            Prices = energyPrices
        };
    }

    private int GetHoursInDay(DateTime date)
    {
        var adjustmentRules = TimeZoneInfo.Local.GetAdjustmentRules();

        // There should only be one adjustment rule for the current timezone.
        if (adjustmentRules.Length > 1)
        {
            return 0;
        }

        var adjustmentRule = adjustmentRules.Single();

        var start = GetTransitionDate(adjustmentRule.DaylightTransitionStart, date.Year);
        if (date == start)
        {
            // This day has 23 hours, and should skip an hour.
            return 23;
        }

        var end = GetTransitionDate(adjustmentRule.DaylightTransitionEnd, date.Year);
        if (date == end)
        {
            // This day has 25 hours, and should add an extra hour.
            return 25;
        }

        // Return default of 24 hours.
        return 24;
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