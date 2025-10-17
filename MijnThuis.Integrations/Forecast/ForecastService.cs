using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Forecast;

public interface IForecastService
{
    Task<ForecastOverview> GetSolarForecastEstimate(decimal latitude, decimal longitude, decimal declination, decimal azimuth, decimal power, byte damping);
}

public class ForecastService : BaseForecastService, IForecastService
{
    private readonly IMemoryCache _memoryCache;

    public ForecastService(
        IConfiguration configuration,
        IMemoryCache memoryCache) : base(configuration)
    {
        _memoryCache = memoryCache;
    }

    public async Task<ForecastOverview> GetSolarForecastEstimate(decimal latitude, decimal longitude, decimal declination, decimal azimuth, decimal power, byte damping)
    {
        try
        {
            return await GetCachedValue($"SOLAR_FORECAST_ESTIMATE[{latitude}|{longitude}|{declination}|{azimuth}|{power}|{damping}]", async () =>
            {
                using var client = InitializeHttpClient();

                var response = await client.GetFromJsonAsync<GetForecastEstimateResponse>($"{_apiKey}/estimate/{latitude}/{longitude}/{declination}/{azimuth}/{power}?damping={damping}");
                var wattHoursPeriodTimes = response.Result.WattHoursPeriod.Keys.Select(x => DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Where(x => x.Day == DateTime.Today.Day);

                return new ForecastOverview
                {
                    EstimatedWattHoursToday = response.Result.WattHoursDay[DateOnly.FromDateTime(DateTime.Today)],
                    EstimatedWattHoursTomorrow = response.Result.WattHoursDay[DateOnly.FromDateTime(DateTime.Today.AddDays(1))],
                    EstimatedWattHoursDayAfterTomorrow = response.Result.WattHoursDay[DateOnly.FromDateTime(DateTime.Today.AddDays(2))],
                    WattHourPeriods = ConvertToWattHourPeriods(response.Result.WattHoursPeriod),
                    Sunrise = wattHoursPeriodTimes.First().TimeOfDay,
                    Sunset = wattHoursPeriodTimes.Last().TimeOfDay
                };
            }, 30);
        }
        catch
        {
            return new ForecastOverview
            {
                EstimatedWattHoursToday = 0,
                EstimatedWattHoursTomorrow = 0,
                EstimatedWattHoursDayAfterTomorrow = 0,
                Sunrise = TimeSpan.Zero,
                Sunset = TimeSpan.Zero
            };
        }
    }

    private List<WattHourPeriod> ConvertToWattHourPeriods(Dictionary<string, int> wattHoursPeriod)
    {
        var periods = new List<WattHourPeriod>();

        return wattHoursPeriod.Select(kvp => new WattHourPeriod
        {
            Timestamp = DateTime.ParseExact(kvp.Key, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            WattHours = kvp.Value
        }).ToList();
    }

    private async Task<T> GetCachedValue<T>(string key, Func<Task<T>> valueFactory, int absoluteExpiration)
    {
        if (_memoryCache.TryGetValue(key, out T value))
        {
            return value;
        }

        value = await valueFactory();
        _memoryCache.Set(key, value, TimeSpan.FromMinutes(absoluteExpiration));

        return value;
    }
}

public class BaseForecastService
{
    protected readonly string _baseAddress;
    protected readonly string _apiKey;

    protected BaseForecastService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("FORECAST_API_BASE_ADDRESS");
        _apiKey = configuration.GetValue<string>("FORECAST_API_KEY");
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

internal class GetForecastEstimateResponse
{
    [JsonPropertyName("result")]
    public GetForecastEstimateResult Result { get; set; }
}

internal class GetForecastEstimateResult
{
    [JsonPropertyName("watts")]
    public Dictionary<string, int> Watts { get; set; }

    [JsonPropertyName("watt_hours_period")]
    public Dictionary<string, int> WattHoursPeriod { get; set; }

    [JsonPropertyName("watt_hours")]
    public Dictionary<string, int> WattHours { get; set; }

    [JsonPropertyName("watt_hours_day")]
    public Dictionary<DateOnly, int> WattHoursDay { get; set; }
}