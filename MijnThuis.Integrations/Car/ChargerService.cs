using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Car;

public interface IChargerService
{
    Task<ChargerOverview> GetChargerOverview(string chargerId);
}

public class ChargerService : BaseChargerService, IChargerService
{
    public ChargerService(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<ChargerOverview> GetChargerOverview(string chargerId)
    {
        using var client = InitializeHttpClient();

        // Initialize cookies
        await client.GetAsync("find-a-charge-point");

        var response = await client.GetFromJsonAsync<BaseChargerResponse>($"api/feature/experienceaccelerator/areas/chargepointmap/getchargepoints/{chargerId}");

        return new ChargerOverview
        {
            ChargerId = response.ChargerPointId,
            Description = response.Address.AddressLine,
            NumberOfChargers = response.Connectors.Count,
            NumberOfChargersAvailable = response.Connectors.Count(x => x.Status == "Available")
        };
    }
}

public class BaseChargerService
{
    private readonly string _baseAddress;

    protected BaseChargerService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("CHARGER_API_BASE_ADDRESS");
    }

    protected HttpClient InitializeHttpClient()
    {
        var cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookies,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        var client = new HttpClient(handler);
        client.BaseAddress = new Uri(_baseAddress);
        client.DefaultRequestHeaders.Add("Accept", "*/*");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
        client.DefaultRequestHeaders.Add("Host", "www.allego.eu");
        cookies.Add(new Uri("https://www.allego.eu"), new Cookie("_ga4789", "map"));

        return client;
    }
}

public class BaseChargerResponse
{
    [JsonPropertyName("address")]
    public ChargerAddress Address { get; set; }

    [JsonPropertyName("evses")]
    public List<Connector> Connectors { get; set; }

    [JsonPropertyName("chargePointId")]
    public string ChargerPointId { get; set; }
}

public class ChargerAddress
{
    [JsonPropertyName("addressLine1")]
    public string AddressLine { get; set; }
}

public class Connector
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
}