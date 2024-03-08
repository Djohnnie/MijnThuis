using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Car;

public interface ICarService
{
    Task<CarOverview> GetOverview();
    Task<BatteryHealth> GetBatteryHealth();
    Task<CarLocation> GetLocation();
    Task<bool> Lock();
    Task<bool> Unlock();
    Task<bool> Honk();
    Task<bool> Fart();
    Task<bool> Preheat();
    Task<bool> StartCharging(int chargingAmps);
    Task<bool> StopCharging();
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
            IsLocked = result.VehicleState.Locked,
            IsCharging = result.ChargeState.ChargingState == "Charging",
            BatteryLevel = result.ChargeState.BatteryLevel,
            RemainingRange = (int)(result.ChargeState.BatteryRange * 1.60934M),
            TemperatureInside = (int)result.ClimateState.InsideTemp,
            TemperatureOutside = (int)result.ClimateState.OutsideTemp,
            IsPreconditioning = result.ClimateState.IsPreconditioning,
            ChargingAmps = result.ChargeState.ChargingAmps,
            MaxChargingAmps = result.ChargeState.MaxChargingAmps,
            IsChargePortOpen = result.ChargeState.IsChargePortOpen
        };
    }

    public async Task<BatteryHealth> GetBatteryHealth()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BatteryHealthResponse>($"battery_health");

        var batteryHealth = result.Results.Single(x => x.Vin == _vinNumber);

        return new BatteryHealth
        {
            Percentage = batteryHealth.Percentage
        };
    }

    public async Task<CarLocation> GetLocation()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<LocationResponse>($"{_vinNumber}/location");

        return new CarLocation
        {
            Address = result.Address,
            Location = result.Location
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
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/lock");

        return result.Result;
    }

    public async Task<bool> Preheat()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/start_climate");

        return result.Result;
    }

    public async Task<bool> StartCharging(int chargingAmps)
    {
        using var client = InitializeHttpClient();

        var result1 = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/set_charging_amps?amps={chargingAmps}");
        var result2 = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/start_charging");

        return result1.Result;
    }

    public async Task<bool> StopCharging()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/stop_charging");

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

    [JsonPropertyName("vehicle_state")]
    public VehicleState VehicleState { get; set; }
}

public class ChargeState
{
    [JsonPropertyName("usable_battery_level")]
    public byte BatteryLevel { get; set; }

    [JsonPropertyName("est_battery_range")]
    public decimal BatteryRange { get; set; }

    [JsonPropertyName("charging_state")]
    public string ChargingState { get; set; }

    [JsonPropertyName("charge_amps")]
    public int ChargingAmps { get; set; }

    [JsonPropertyName("charge_current_request_max")]
    public int MaxChargingAmps { get; set; }

    [JsonPropertyName("charge_port_door_open")]
    public bool IsChargePortOpen { get; set; }
}

public class ClimateState
{
    [JsonPropertyName("inside_temp")]
    public decimal InsideTemp { get; set; }

    [JsonPropertyName("outside_temp")]
    public decimal OutsideTemp { get; set; }

    [JsonPropertyName("is_preconditioning")]
    public bool IsPreconditioning { get; set; }
}

public class VehicleState
{
    [JsonPropertyName("locked")]
    public bool Locked { get; set; }
}

public class LocationResponse
{
    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("saved_location")]
    public string Location { get; set; }
}

public class BatteryHealthResponse
{
    [JsonPropertyName("results")]
    public List<VehicleBatteryHealth> Results { get; set; }
}

public class VehicleBatteryHealth
{
    [JsonPropertyName("vin")]
    public string Vin { get; set; }

    [JsonPropertyName("health_percent")]
    public decimal Percentage { get; set; }
}