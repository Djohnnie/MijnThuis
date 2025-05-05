using Microsoft.Extensions.Configuration;
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

        return new EnergyPricesForDate
        {
            Date = date,
            Prices = period.Points.Select(p => new EnergyPrice
            {
                TimeStamp = date.AddHours(period.Resolution == "PT15M" ? (p.Position - 1) / 4 : p.Position - 1),
                Price = p.Price
            }).ToList()
        };
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
    public TimeSeries[] TimeSeries { get; set; }
}

public class TimeSeries
{
    [XmlElement("Period")]
    public Period[] Periods { get; set; }
}

public class Period
{
    [XmlElement("resolution")]
    public string Resolution { get; set; }

    [XmlElement("Point")]
    public Point[] Points { get; set; }
}

public class Point
{
    [XmlElement("position")]
    public int Position { get; set; }

    [XmlElement("price.amount")]
    public decimal Price { get; set; }
}