using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
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

        var request = new GetChargersRequest
        {
            Latitude = 51.05M,
            Longitude = 4.35M,
            Types = ["type2", "type2_cable"],
            Radius = 800,
            Limit = 10,
            GetStatus = true
        };

        var response = await client.PostAsJsonAsync("1/get_chargers", request);
        var results = await response.Content.ReadFromJsonAsync<GetChargersResponse>();
        var charger = results.Chargers.SingleOrDefault(x => $"{x.Id}" == chargerId);

        return new ChargerOverview
        {
            ChargerId = $"{charger.Id}",
            Description = charger.Name,
            NumberOfChargers = charger.Connectors.Count,
            NumberOfChargersAvailable = charger.Connectors.Count(x => x.Status == "OPERATIONAL")
        };
    }
}

public class BaseChargerService
{
    private readonly string _baseAddress;
    private readonly string _apiKey;

    protected BaseChargerService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("CHARGER_API_BASE_ADDRESS");
        _apiKey = configuration.GetValue<string>("CHARGER_API_KEY");
    }

    protected HttpClient InitializeHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(_baseAddress)
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("APIKEY", _apiKey);

        return client;
    }
}

public class GetChargersRequest
{
    [JsonPropertyName("lat")]
    public decimal Latitude { get; set; }

    [JsonPropertyName("lon")]
    public decimal Longitude { get; set; }

    [JsonPropertyName("types")]
    public string[] Types { get; set; }

    [JsonPropertyName("radius")]
    public decimal Radius { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("get_status")]
    public bool GetStatus { get; set; }
}

public class GetChargersResponse
{
    [JsonPropertyName("result")]
    public List<ChargerResponse> Chargers { get; set; }
}

public class ChargerResponse
{
    [JsonPropertyName("name_without_network")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("outlets")]
    public List<Connector> Connectors { get; set; }
}

public class Connector
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
}