using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Solar;

public interface ISolarService
{
    Task<SolarOverview> GetOverview();

    Task<BatteryLevel> GetBatteryLevel();
}

public class SolarService : BaseService, ISolarService
{
    private readonly string _authToken;
    private readonly string _siteId;

    public SolarService(IConfiguration configuration) : base(configuration)
    {
        _authToken = configuration.GetValue<string>("SOLAR_API_AUTH_TOKEN");
        _siteId = configuration.GetValue<string>("SOLAR_SITE_ID");
    }

    public async Task<SolarOverview> GetOverview()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<OverviewResponse>($"site/{_siteId}/overview?api_key={_authToken}");

        return new SolarOverview
        {
            CurrentPower = result.Overview.CurrentPower.Power,
            LastDayEnergy = result.Overview.LastDayData.Energy,
            LastMonthEnergy = result.Overview.LastMonthData.Energy,
        };
    }

    public async Task<BatteryLevel> GetBatteryLevel()
    {
        using var client = InitializeHttpClient();
        var now = TimeProvider.System.GetLocalNow();
        var startTime = $"{now.AddMinutes(-15):yyyy-MM-dd HH:mm:00}";
        var endTime = $"{now:yyyy-MM-dd HH:mm:00}";

        try
        {
            var result = await client.GetFromJsonAsync<StorageOverview>($"site/{_siteId}/storageData?api_key={_authToken}&startTime={startTime}&endTime={endTime}");

            return new BatteryLevel
            {
                Level = result.Storage.Batteries.Single().Telemetries.Last().Level,
                Health = result.Storage.Batteries.Single().Telemetries.Last().EnergyAvailable / result.Storage.Batteries.Single().Nameplate
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }

    }
}

public class BaseService
{
    private readonly string _baseAddress;

    protected BaseService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("SOLAR_API_BASE_ADDRESS");
    }

    protected HttpClient InitializeHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseAddress);

        return client;
    }
}

public class OverviewResponse
{
    [JsonPropertyName("overview")]
    public Overview Overview { get; set; }
}

public class Overview
{
    [JsonPropertyName("lastDayData")]
    public EnergyData LastDayData { get; set; }

    [JsonPropertyName("lastMonthData")]
    public EnergyData LastMonthData { get; set; }

    [JsonPropertyName("currentPower")]
    public PowerData CurrentPower { get; set; }
}

public class EnergyData
{
    [JsonPropertyName("energy")]
    public decimal Energy { get; set; }
}

public class PowerData
{
    [JsonPropertyName("power")]
    public decimal Power { get; set; }
}

public class StorageOverview
{
    [JsonPropertyName("storageData")]
    public Storage Storage { get; init; }
}

public class Storage
{
    [JsonPropertyName("batteries")]
    public List<Battery> Batteries { get; init; }
}

public class Battery
{
    [JsonPropertyName("nameplate")]
    public decimal Nameplate { get; init; }

    [JsonPropertyName("telemetries")]
    public List<Telemetry> Telemetries { get; init; }
}

public class Telemetry
{
    [JsonPropertyName("batteryPercentageState")]
    public decimal Level { get; init; }

    [JsonPropertyName("fullPackEnergyAvailable")]
    public decimal EnergyAvailable { get; init; }
}