using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Car;

public interface IChargerService
{
    Task<ChargerOverview> GetChargerOverview(string chargerId);
}

public class ChargerService : BaseChargerService, IChargerService
{
    public ChargerService(IConfiguration configuration) : base(configuration) { }

    public async Task<ChargerOverview> GetChargerOverview(string chargerId)
    {
        try
        {
            using var client = InitializeHttpClient();

            var response = await client.GetFromJsonAsync<GetChargersResponse>($"api/chargepoint?id={chargerId}");

            if (response.Chargepoint != null)
            {
                return new ChargerOverview
                {
                    ChargerId = $"{response.Chargepoint.Id}",
                    Description = response.Chargepoint.Name,
                    NumberOfChargers = response.Chargepoint.Connectors.Count,
                    NumberOfChargersAvailable = response.Chargepoint.Connectors.Count(x => x.Status == "Available")
                };
            }

            return new ChargerOverview
            {
                ChargerId = chargerId,
                Description = "Unknown",
                NumberOfChargers = 0,
                NumberOfChargersAvailable = 0
            };
        }
        catch
        {
            return new ChargerOverview
            {
                ChargerId = chargerId,
                Description = "Unknown",
                NumberOfChargers = 0,
                NumberOfChargersAvailable = 0
            };
        }
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
        var client = new HttpClient
        {
            BaseAddress = new Uri(_baseAddress)
        };

        return client;
    }
}

public class GetChargersResponse
{
    [JsonPropertyName("chargepoint")]
    public Chargepoint Chargepoint { get; set; }
}

public class Chargepoint
{
    [JsonPropertyName("uid")]
    public int Id { get; set; }

    [JsonPropertyName("externalId")]
    public string Name { get; set; }

    [JsonPropertyName("evses")]
    public List<Connector> Connectors { get; set; }
}

public class Connector
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
}