using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static IdentityModel.ClaimComparer;

namespace MijnThuis.Integrations.Solar;

public interface ISolarService
{
    Task<SolarOverview> GetOverview();

    Task<BatteryLevel> GetBatteryLevel();

    Task<EnergyProduced> GetEnergy();

    Task<EnergyOverviewResponse> GetEnergyOverview(DateTime date);

    Task<PowerOverviewResponse> GetPowerOverview(DateTime date);

    Task<BatteryOverviewResponse> GetBatteryOverview(DateTime date);

    Task<EnergyOverview> GetEnergyToday();

    Task<EnergyOverview> GetEnergyThisMonth();

    Task<StorageData> GetStorageData(StorageDataRange range);
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
        using var client = await InitializeAuthenticatedHttpClient();

        var result = await client.GetFromJsonAsync<PowerflowResponse>($"services/m/so/dashboard/v2/site/{_siteId}/powerflow/latest/?components=consumption,grid,storage");

        var currentSolarPower = 0M;
        var currentBatteryPower = 0M;
        var currentGridPower = 0M;

        if (result.SolarProduction.IsProducing)
        {
            currentSolarPower = result.SolarProduction.CurrentPower;
        }

        if (result.Storage.Status == "charging")
        {
            currentBatteryPower = result.Storage.CurrentPower;
        }
        else
        {
            currentBatteryPower = -result.Storage.CurrentPower;
        }

        if (result.Grid.Status == "import")
        {
            currentGridPower = result.Grid.CurrentPower;
        }
        else
        {
            currentGridPower = -result.Grid.CurrentPower;
        }

        return new SolarOverview
        {
            CurrentSolarPower = currentSolarPower,
            CurrentBatteryPower = currentBatteryPower,
            CurrentGridPower = currentGridPower,
            CurrentConsumptionPower = result.Consumption.CurrentPower,
            BatteryLevel = result.Storage.ChargeLevel,
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
                Level = result.Storage.Batteries.Single().Telemetries.Last().Level ?? 0M,
                Health = (result.Storage.Batteries.Single().Telemetries.Last().EnergyAvailable ?? 0M) / result.Storage.Batteries.Single().Nameplate * 100M,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);

            return new BatteryLevel
            {
                Level = 0,
                Health = 0
            };
        }
    }

    public async Task<EnergyProduced> GetEnergy()
    {
        var today = TimeProvider.System.GetLocalNow().Date;
        var begin = $"{new DateTime(today.Year, today.Month, 1):yyyy-MM-dd}";
        var end = $"{new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1):yyyy-MM-dd}";

        using var client = await InitializeAuthenticatedHttpClient();
        var response = await client.GetFromJsonAsync<EnergyOverviewResponse>($"services/dashboard/energy/sites/{_siteId}?start-date={begin}&end-date={end}&chart-time-unit=days&measurement-types=production,consumption,production-distribution-with-storage,consumption-distribution-with-storage,import,export");

        return new EnergyProduced
        {
            LastDayEnergy = response.Chart.Measurements.SingleOrDefault(x => x.MeasurementTime.Date == today)?.Production ?? 0M,
            LastMonthEnergy = response.Chart.Measurements.Where(x => x.Production.HasValue).Sum(x => x.Production) ?? 0M
        };
    }

    public async Task<EnergyOverviewResponse> GetEnergyOverview(DateTime date)
    {
        var begin = $"{new DateTime(date.Year, date.Month, 1):yyyy-MM-dd}";
        var end = $"{new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1):yyyy-MM-dd}";

        using var client = await InitializeAuthenticatedHttpClient();
        var response = await client.GetFromJsonAsync<EnergyOverviewResponse>($"services/dashboard/energy/sites/{_siteId}?start-date={begin}&end-date={end}&chart-time-unit=days&measurement-types=production,consumption,production-distribution-with-storage,consumption-distribution-with-storage,import,export");

        return response;
    }

    public async Task<PowerOverviewResponse> GetPowerOverview(DateTime date)
    {
        var begin = $"{date:yyyy-MM-dd}";
        var end = $"{date:yyyy-MM-dd}";

        using var client = await InitializeAuthenticatedHttpClient();
        var response = await client.GetFromJsonAsync<PowerOverviewResponse>($"services/dashboard/power/sites/{_siteId}?start-date={begin}&end-date={end}&chart-time-unit=quarter-hours&measurement-types=production,consumption,production-distribution-with-storage,consumption-distribution-with-storage,import,export,storage-charge-level");

        return response;
    }

    public async Task<BatteryOverviewResponse> GetBatteryOverview(DateTime date)
    {
        var start = $"{date:yyyy-MM-dd HH:mm:00}";
        var end = $"{date.AddDays(1).AddSeconds(-1):yyyy-MM-dd HH:mm:00}";

        using var client = InitializeHttpClient();
        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.Converters.Add(new JsonDateTimeConverter());
        var response = await client.GetFromJsonAsync<BatteryOverviewResponse>($"site/{_siteId}/storageData?api_key={_authToken}&startTime={start}&endTime={end}", serializerOptions);

        return response;
    }

    public async Task<EnergyOverview> GetEnergyToday()
    {
        var today = TimeProvider.System.GetLocalNow().Date;
        var start = $"{today:yyyy-MM-dd HH:mm:00}";
        var end = $"{today.AddDays(1).AddSeconds(-1):yyyy-MM-dd HH:mm:00}";

        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<EnergyResponse>($"site/{_siteId}/energyDetails?api_key={_authToken}&startTime={start}&endTime={end}&timeUnit=DAY");

        return new EnergyOverview
        {
            Produced = result.EnergyDetails.Meters.Single(m => m.Type == "Production").Values.Sum(v => v.Value),
            Consumed = result.EnergyDetails.Meters.Single(m => m.Type == "Consumption").Values.Sum(v => v.Value),
            Purchased = result.EnergyDetails.Meters.Single(m => m.Type == "Purchased").Values.Sum(v => v.Value),
        };
    }

    public async Task<EnergyOverview> GetEnergyThisMonth()
    {
        var today = TimeProvider.System.GetLocalNow().Date;
        var start = $"{new DateTime(today.Year, today.Month, 1):yyyy-MM-dd HH:mm:00}";
        var end = $"{new DateTime(today.Year, today.Month, 1, 23, 59, 59).AddMonths(1).AddDays(-1):yyyy-MM-dd HH:mm:00}";

        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<EnergyResponse>($"site/{_siteId}/energyDetails?api_key={_authToken}&startTime={start}&endTime={end}&timeUnit=MONTH");

        return new EnergyOverview
        {
            Produced = result.EnergyDetails.Meters.Single(m => m.Type == "Production").Values.Sum(v => v.Value),
            Consumed = result.EnergyDetails.Meters.Single(m => m.Type == "Consumption").Values.Sum(v => v.Value),
            Purchased = result.EnergyDetails.Meters.Single(m => m.Type == "Purchased").Values.Sum(v => v.Value),
        };
    }

    public async Task<StorageData> GetStorageData(StorageDataRange range)
    {
        var today = TimeProvider.System.GetLocalNow().Date;
        var start = $"{today:yyyy-MM-dd HH:mm:ss}";
        var end = $"{today.AddDays(1).AddSeconds(-1):yyyy-MM-dd HH:mm:ss}";

        switch (range)
        {
            case StorageDataRange.ThreeDays:
                start = $"{today.AddDays(-2):yyyy-MM-dd HH:mm:ss}";
                break;
            case StorageDataRange.Week:
                start = $"{today.AddDays(-5):yyyy-MM-dd HH:mm:ss}";
                break;
            case StorageDataRange.Month:
                start = $"{today.AddDays(-28):yyyy-MM-dd HH:mm:ss}";
                break;
        }

        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<StorageOverview>($"site/{_siteId}/storageData?api_key={_authToken}&startTime={start}&endTime={end}");

        return new StorageData
        {
            Entries = result.Storage.Batteries.Single().Telemetries.Select(x => new StorageDataEntry
            {
                //Timestamp = $"{x.TimeStamp:HH:mm}",
                ChargeState = x.Level ?? 0M
            }).ToList()
        };
    }
}

public class BaseService
{
    private readonly string _baseAddress;
    private readonly string _appBaseAddress;
    private readonly string _authUsername;
    private readonly string _authPassword;

    private SolarAuth _authToken;

    protected BaseService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("SOLAR_API_BASE_ADDRESS");
        _appBaseAddress = configuration.GetValue<string>("SOLAR_APP_BASE_ADDRESS");
        _authUsername = configuration.GetValue<string>("SOLAR_APP_AUTH_USERNAME");
        _authPassword = configuration.GetValue<string>("SOLAR_APP_AUTH_PASSWORD");
    }

    protected HttpClient InitializeHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseAddress);

        return client;
    }

    protected async Task<HttpClient> InitializeAuthenticatedHttpClient()
    {
        if (_authToken == null || _authToken.ExpiresOn >= DateTime.Now)
        {
            _authToken = await GetAuthToken();
        }

        var client = new HttpClient();
        client.BaseAddress = new Uri(_appBaseAddress);
        client.DefaultRequestHeaders.Add("x-csrf-token", _authToken.CsrfToken);
        client.DefaultRequestHeaders.Add("client-version", "3.12");
        client.DefaultRequestHeaders.Add("Cookie", _authToken.Cookies);

        return client;
    }

    private async Task<SolarAuth> GetAuthToken()
    {
        var cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookies
        };

        using var authClient = new HttpClient(handler);
        var response = await authClient.PostAsync($"{_appBaseAddress}/solaredge-apigw/api/login?j_username={_authUsername}&j_password={_authPassword}", null);

        var csrf_token = cookies.GetCookies(new Uri("https://api.solaredge.com")).Cast<Cookie>().FirstOrDefault(x => x.Name == "CSRF-TOKEN")?.Value;

        var cookieBuilder = new StringBuilder();

        foreach (var cookie in cookies.GetAllCookies().Cast<Cookie>())
        {
            cookieBuilder.Append($"{cookie.Name}={cookie.Value}; ");
        }

        return new SolarAuth
        {
            Cookies = cookieBuilder.ToString(),
            CsrfToken = csrf_token,
            ExpiresOn = DateTime.Now.AddHours(24)
        };
    }
}

public class SolarAuth
{
    public string Cookies { get; set; }

    public string CsrfToken { get; set; }

    public DateTime ExpiresOn { get; set; }
}

public class PowerflowResponse
{
    [JsonPropertyName("solarProduction")]
    public SolarProduction SolarProduction { get; set; }

    [JsonPropertyName("storage")]
    public StorageDetails Storage { get; set; }

    [JsonPropertyName("grid")]
    public GridDetails Grid { get; set; }

    [JsonPropertyName("consumption")]
    public ConsumptionDetails Consumption { get; set; }
}

public class SolarProduction
{
    [JsonPropertyName("currentPower")]
    public decimal CurrentPower { get; set; }

    [JsonPropertyName("isProducing")]
    public bool IsProducing { get; set; }
}

public class StorageDetails
{
    [JsonPropertyName("currentPower")]
    public decimal CurrentPower { get; set; }

    [JsonPropertyName("chargeLevel")]
    public int ChargeLevel { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
}

public class GridDetails
{
    [JsonPropertyName("currentPower")]
    public decimal CurrentPower { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
}

public class ConsumptionDetails
{
    [JsonPropertyName("currentPower")]
    public decimal CurrentPower { get; set; }
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
    [JsonPropertyName("timeStamp")]
    public DateTime TimeStamp { get; set; }

    [JsonPropertyName("batteryPercentageState")]
    public decimal? Level { get; init; }

    [JsonPropertyName("fullPackEnergyAvailable")]
    public decimal? EnergyAvailable { get; init; }
}

public class EnergyProducedResponse
{
    [JsonPropertyName("energyProducedOverviewList")]
    public List<EnergyProducedPeriodResponse> EnergyProducedOverviewList { get; set; }
}

public class EnergyProducedPeriodResponse
{
    [JsonPropertyName("timePeriod")]
    public string TimePeriod { get; set; }

    [JsonPropertyName("energy")]
    public decimal Energy { get; set; }
}

public class EnergyResponse
{
    [JsonPropertyName("energyDetails")]
    public EnergyDetails EnergyDetails { get; set; }
}

public class EnergyDetails
{
    [JsonPropertyName("meters")]
    public List<EnergyMeter> Meters { get; set; }
}

public class EnergyMeter
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("values")]
    public List<EnergyValue> Values { get; set; }
}

public class EnergyValue
{
    [JsonPropertyName("value")]
    public decimal Value { get; set; }
}

public class EnergyOverviewResponse
{
    [JsonPropertyName("summary")]
    public EnergySummary Summary { get; set; }

    [JsonPropertyName("chart")]
    public EnergyChart Chart { get; set; }
}

public class PowerOverviewResponse
{
    [JsonPropertyName("measurements")]
    public List<PowerMeasurement> Measurements { get; set; }
}

public class BatteryOverviewResponse
{
    [JsonPropertyName("storageData")]
    public Storage Storage { get; init; }
}

public class EnergySummary
{
    [JsonPropertyName("production")]
    public decimal Production { get; set; }
}

public class EnergyChart
{
    [JsonPropertyName("measurements")]
    public List<EnergyMeasurement> Measurements { get; set; }
}

public class EnergyMeasurement
{
    [JsonPropertyName("measurementTime")]
    public DateTime MeasurementTime { get; set; }

    [JsonPropertyName("production")]
    public decimal? Production { get; set; }

    [JsonPropertyName("productionDistribution")]
    public ProductionDistribution ProductionDistribution { get; set; }

    [JsonPropertyName("consumption")]
    public decimal? Consumption { get; set; }

    [JsonPropertyName("consumptionDistribution")]
    public ConsumptionDistribution ConsumptionDistribution { get; set; }

    [JsonPropertyName("export")]
    public decimal? Export { get; set; }

    [JsonPropertyName("import")]
    public decimal? Import { get; set; }
}

public class PowerMeasurement
{
    [JsonPropertyName("measurementTime")]
    public DateTime MeasurementTime { get; set; }

    [JsonPropertyName("production")]
    public decimal? Production { get; set; }

    [JsonPropertyName("productionDistribution")]
    public ProductionDistribution ProductionDistribution { get; set; }

    [JsonPropertyName("consumption")]
    public decimal? Consumption { get; set; }

    [JsonPropertyName("consumptionDistribution")]
    public ConsumptionDistribution ConsumptionDistribution { get; set; }

    [JsonPropertyName("storageLevel")]
    public decimal? StorageLevel { get; set; }

    [JsonPropertyName("export")]
    public decimal? Export { get; set; }

    [JsonPropertyName("import")]
    public decimal? Import { get; set; }
}

public class ProductionDistribution
{
    [JsonPropertyName("productionToHome")]
    public decimal? ToHome { get; set; }

    [JsonPropertyName("productionToBattery")]
    public decimal? ToBattery { get; set; }

    [JsonPropertyName("productionToGrid")]
    public decimal? ToGrid { get; set; }
}

public class ConsumptionDistribution
{
    [JsonPropertyName("consumptionFromBattery")]
    public decimal? FromBattery { get; set; }

    [JsonPropertyName("consumptionFromSolar")]
    public decimal? FromSolar { get; set; }

    [JsonPropertyName("consumptionFromGrid")]
    public decimal? FromGrid { get; set; }
}

public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(DateTime));
        return DateTime.Parse(reader.GetString() ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}