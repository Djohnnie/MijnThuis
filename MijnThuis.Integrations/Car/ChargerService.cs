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
    private readonly string _sessionId;

    public ChargerService(IConfiguration configuration) : base(configuration)
    {
        _sessionId = configuration.GetValue<string>("CHARGER_SESSION_ID");
    }

    public async Task<ChargerOverview> GetChargerOverview(string chargerId)
    {
        using var client = InitializeHttpClient();

        var response = await client.GetFromJsonAsync<GetChargersResponse>($"1/get_chargers?ids={chargerId}&get_status=true&session_id={_sessionId}");
        var result = response.Chargers.Single(x => $"{x.Id}" == chargerId);

        return new ChargerOverview
        {
            ChargerId = $"{result.Id}",
            Description = result.Name,
            NumberOfChargers = result.Connectors.Count,
            NumberOfChargersAvailable = result.Connectors.Count(x => x.Status == "AVAILABLE")
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