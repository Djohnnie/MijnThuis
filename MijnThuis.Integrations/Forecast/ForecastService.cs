using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Forecast;

public interface IForecastService
{
    Task<ForecastOverview> GetSolarForecastEstimate(decimal latitude, decimal longitude, decimal declination, decimal azimuth, decimal power);
}

public class ForecastService : BaseForecastService, IForecastService
{
    public ForecastService(IConfiguration configuration) : base(configuration)
    {

    }

    public async Task<ForecastOverview> GetSolarForecastEstimate(decimal latitude, decimal longitude, decimal declination, decimal azimuth, decimal power)
    {
        try
        {
            using var client = InitializeHttpClient();

            var response = await client.GetFromJsonAsync<GetForecastEstimateResponse>($"{_apiKey}/estimate/{latitude}/{longitude}/{declination}/{azimuth}/{power}?damping=1");
            var wattHoursPeriodTimes = response.Result.WattHoursPeriod.Keys.Select(x => DateTime.ParseExact(x, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Where(x => x.Day == DateTime.Today.Day);

            return new ForecastOverview
            {
                EstimatedWattHoursToday = response.Result.WattHoursDay[DateOnly.FromDateTime(DateTime.Today)],
                EstimatedWattHoursTomorrow = response.Result.WattHoursDay[DateOnly.FromDateTime(DateTime.Today.AddDays(1))],
                Sunrise = wattHoursPeriodTimes.First().TimeOfDay,
                Sunset = wattHoursPeriodTimes.Last().TimeOfDay
            };
        }
        catch
        {
            return new ForecastOverview
            {
                EstimatedWattHoursToday = 0,
                EstimatedWattHoursTomorrow = 0,
                Sunrise = TimeSpan.Zero,
                Sunset = TimeSpan.Zero
            };
        }
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