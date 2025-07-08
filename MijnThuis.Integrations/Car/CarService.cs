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
    Task<CarCharges> GetCharges();
    Task<CarDrives> GetDrives();
    Task<bool> Lock();
    Task<bool> Unlock();
    Task<bool> Honk();
    Task<bool> Fart();
    Task<bool> Preheat();
    Task<bool> StartCharging(int chargingAmps);
    Task<bool> StopCharging();
    Task<bool> SetCabinHeatProtection(bool enable, bool fanOnly);
    Task<bool> SetCabinHeatProtectionTemperature(int temperature);
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
            IsCabinOverheatProtection = result.ClimateState.CabinOverheatProtection == "On",
            ChargingAmps = result.ChargeState.ChargingAmps,
            MaxChargingAmps = result.ChargeState.MaxChargingAmps,
            ChargeLimit = result.ChargeState.ChargeLimit,
            IsChargePortOpen = result.ChargeState.IsChargePortOpen,
            ChargeEnergyAdded = result.ChargeState.ChargeEnergyAdded,
            ChargeRangeAdded = result.ChargeState.ChargeMilesAdded * 1.60934M,
        };
    }

    public async Task<BatteryHealth> GetBatteryHealth()
    {
        try
        {
            using var client = InitializeHttpClient();
            var result = await client.GetFromJsonAsync<BatteryHealthResponse>($"battery_health");

            var batteryHealth = result.Results.Single(x => x.Vin == _vinNumber);

            return new BatteryHealth
            {
                Percentage = batteryHealth.Percentage
            };
        }
        catch
        {
            return new BatteryHealth { Percentage = 0 };
        }
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

    public async Task<CarCharges> GetCharges()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<ChargesResponse>($"{_vinNumber}/charges?distance_format=km&format=json");

        return new CarCharges
        {
            Charges = result.Results.Select(c => new CarCharge
            {
                Id = c.Id,
                StartedAt = DateTimeOffset.FromUnixTimeSeconds(c.StartedAt).ToLocalTime().DateTime,
                EndedAt = DateTimeOffset.FromUnixTimeSeconds(c.EndedAt).ToLocalTime().DateTime,
                Location = c.Location,
                LocationFriendlyName = c.LocationFriendlyName,
                IsSupercharger = c.IsSupercharger,
                IsFastCharger = c.IsFastCharger,
                Odometer = (int)Math.Round(c.Odometer),
                EnergyAdded = c.EnergyAdded,
                EnergyUsed = c.EnergyUsed,
                RangeAdded = (int)Math.Round(c.RangeAdded),
                BatteryLevelStart = c.BatteryLevelStart,
                BatteryLevelEnd = c.BatteryLevelEnd,
                DistanceSinceLastCharge = (int)Math.Round(c.DistanceSinceLastCharge)
            }).ToList()
        };
    }

    public async Task<CarDrives> GetDrives()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<DrivesResponse>($"{_vinNumber}/drives?distance_format=km&temperature_format=c");

        return new CarDrives
        {
            Drives = result.Results.Select(c => new CarDrive
            {
                Id = c.Id,
                StartedAt = DateTimeOffset.FromUnixTimeSeconds(c.StartedAt).ToLocalTime().DateTime,
                EndedAt = DateTimeOffset.FromUnixTimeSeconds(c.EndedAt).ToLocalTime().DateTime,
                StartingLocation = c.StartingLocation,
                EndingLocation = c.EndingLocation,
                StartingOdometer = (int)Math.Round(c.StartingOdometer),
                EndingOdometer = (int)Math.Round(c.EndingOdometer),
                StartingBattery = c.StartingBattery,
                EndingBattery = c.EndingBattery,
                EnergyUsed = c.EnergyUsed,
                RangeUsed = (int)Math.Round(c.RangeUsed),
                AverageSpeed = c.AverageSpeed,
                MaximumSpeed = c.MaximumSpeed,
                Distance = (int)Math.Round(c.Distance),
                AverageInsideTemperature = c.AverageInsideTemperature,
                AverageOutsideTemperature = c.AverageOutsideTemperature
            }).ToList()
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
        _ = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/set_charging_amps?amps={chargingAmps}");
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/start_charging");

        return result.Result;
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

    public async Task<bool> SetCabinHeatProtection(bool enable, bool fanOnly)
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/set_cabin_overheat_protection?on={(enable ? "true" : "false")}&fan_only={(fanOnly ? "true" : "false")}");

        return result.Result;
    }

    public async Task<bool> SetCabinHeatProtectionTemperature(int temperature)
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>($"{_vinNumber}/command/set_cop_temp?temperature={temperature}");

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

    [JsonPropertyName("charger_actual_current")]
    public int ChargingAmps { get; set; }

    [JsonPropertyName("charge_current_request_max")]
    public int MaxChargingAmps { get; set; }

    [JsonPropertyName("charge_limit_soc")]
    public int ChargeLimit { get; set; }

    [JsonPropertyName("charge_port_door_open")]
    public bool IsChargePortOpen { get; set; }

    [JsonPropertyName("charge_energy_added")]
    public decimal ChargeEnergyAdded { get; set; }

    [JsonPropertyName("charge_miles_added_rated")]
    public decimal ChargeMilesAdded { get; set; }
}

public class ClimateState
{
    [JsonPropertyName("inside_temp")]
    public decimal InsideTemp { get; set; }

    [JsonPropertyName("outside_temp")]
    public decimal OutsideTemp { get; set; }

    [JsonPropertyName("is_preconditioning")]
    public bool IsPreconditioning { get; set; }

    [JsonPropertyName("cabin_overheat_protection")]
    public string CabinOverheatProtection { get; set; }
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

public class ChargesResponse
{
    [JsonPropertyName("results")]
    public List<ChargeResult> Results { get; set; }
}

public class ChargeResult
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("started_at")]
    public long StartedAt { get; set; }

    [JsonPropertyName("ended_at")]
    public long EndedAt { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; }

    [JsonPropertyName("saved_location")]
    public string LocationFriendlyName { get; set; }

    [JsonPropertyName("is_supercharger")]
    public bool IsSupercharger { get; set; }

    [JsonPropertyName("is_fast_charger")]
    public bool IsFastCharger { get; set; }

    [JsonPropertyName("odometer")]
    public decimal Odometer { get; set; }

    [JsonPropertyName("energy_added")]
    public decimal EnergyAdded { get; set; }

    [JsonPropertyName("energy_used")]
    public decimal EnergyUsed { get; set; }

    [JsonPropertyName("miles_added")]
    public decimal RangeAdded { get; set; }

    [JsonPropertyName("starting_battery")]
    public int BatteryLevelStart { get; set; }

    [JsonPropertyName("ending_battery")]
    public int BatteryLevelEnd { get; set; }

    [JsonPropertyName("since_last_charge")]
    public decimal DistanceSinceLastCharge { get; set; }
}

public class DrivesResponse
{
    [JsonPropertyName("results")]
    public List<DriveResult> Results { get; set; }
}

public class DriveResult
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("started_at")]
    public long StartedAt { get; set; }

    [JsonPropertyName("ended_at")]
    public long EndedAt { get; set; }

    [JsonPropertyName("starting_location")]
    public string StartingLocation { get; set; }

    [JsonPropertyName("ending_location")]
    public string EndingLocation { get; set; }

    [JsonPropertyName("starting_odometer")]
    public decimal StartingOdometer { get; set; }

    [JsonPropertyName("ending_odometer")]
    public decimal EndingOdometer { get; set; }

    [JsonPropertyName("starting_battery")]
    public int StartingBattery { get; set; }

    [JsonPropertyName("ending_battery")]
    public int EndingBattery { get; set; }

    [JsonPropertyName("energy_used")]
    public decimal EnergyUsed { get; set; }

    [JsonPropertyName("rated_range_used")]
    public decimal RangeUsed { get; set; }

    [JsonPropertyName("average_speed")]
    public int AverageSpeed { get; set; }

    [JsonPropertyName("max_speed")]
    public int MaximumSpeed { get; set; }

    [JsonPropertyName("odometer_distance")]
    public decimal Distance { get; set; }

    [JsonPropertyName("average_inside_temperature")]
    public decimal AverageInsideTemperature { get; set; }

    [JsonPropertyName("average_outside_temperature")]
    public decimal AverageOutsideTemperature { get; set; }
}