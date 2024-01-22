using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Car;

public interface ICarService
{
    Task<CarOverview> GetOverview();
    Task<bool> Lock();
    Task<bool> Unlock();
    Task<bool> Honk();
    Task<bool> Fart();
    Task<bool> OpenFrunk();
    Task<bool> ToggleTrunk();
}

public class CarService : BaseService, ICarService
{
    private readonly string _vinNumber;

    public CarService(IConfiguration configuration) : base(configuration)
    {
        _vinNumber = configuration.GetValue<string>("CAR_VIN_NUMBER");
    }

    public async Task<CarOverview> GetOverview()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<StateResponse>($"{_vinNumber}/state");

        return new CarOverview
        {
            State = "Parked",
            BatteryLevel = result.ChargeState.BatteryLevel,
            RemainingRange = (int)result.ChargeState.BatteryRange,
            TemperatureInside = (int)result.ClimateState.InsideTemp,
            TemperatureOutside = (int)result.ClimateState.OutsideTemp,
            Location = "Home"
        };
    }

    public async Task<bool> Fart()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/remote_boombox");

        return result.Result;
    }

    public async Task<bool> Honk()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/remote_boombox");

        return result.Result;
    }

    public async Task<bool> Lock()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/remote_boombox");

        return result.Result;
    }

    public async Task<bool> OpenFrunk()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/remote_boombox");

        return result.Result;
    }

    public async Task<bool> ToggleTrunk()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/lock");

        return result.Result;
    }

    public async Task<bool> Unlock()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/unlock");

        return result.Result;
    }
}

public class BaseService
{
    private readonly string _baseAddress;
    private readonly string _authToken;

    protected BaseService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("CAR_API_BASE_ADDRESS");
        _authToken = configuration.GetValue<string>("CAR_API_AUTH_TOKEN");
    }

    protected HttpClient InitializeHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseAddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

        return client;
    }
}

public class BaseResponse
{
    public bool Result { get; set; }
}

public class StateResponse
{
    [JsonPropertyName("charge_state")]
    public ChargeState ChargeState { get; set; }

    [JsonPropertyName("climate_state")]
    public ClimateState ClimateState { get; set; }
}

public class ChargeState
{
    [JsonPropertyName("usable_battery_level")]
    public byte BatteryLevel { get; set; }

    [JsonPropertyName("battery_range")]
    public decimal BatteryRange { get; set; }
}

public class ClimateState
{
    [JsonPropertyName("inside_temp")]
    public decimal InsideTemp { get; set; }

    [JsonPropertyName("outside_temp")]
    public decimal OutsideTemp { get; set; }
}