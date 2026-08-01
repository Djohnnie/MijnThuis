using Microsoft.Extensions.Configuration;
using MijnThuis.Integrations.Airconditioning.Gree;
using System.Collections.Concurrent;
using System.Net;

namespace MijnThuis.Integrations.Airconditioning;

public interface IAirconditioningService
{
    Task<AirconditioningOverview> GetOverview();

    Task<bool> TurnOn();

    Task<bool> TurnOff();

    Task<bool> SetTargetTemperature(int temperature);

    Task<bool> IncreaseTargetTemperature();

    Task<bool> DecreaseTargetTemperature();
}

/// <summary>
/// Controls a Gree Wi-Fi air conditioning unit using the reverse-engineered UDP protocol
/// described in https://github.com/tomikaa87/gree-remote. The unit's IP and MAC address are
/// fixed (configured), so no network discovery is needed - only binding (to obtain the
/// device's encryption key) and status/control requests.
/// </summary>
public class AirconditioningService : IAirconditioningService
{
    private const string ColPower = "Pow";
    private const string ColMode = "Mod";
    private const string ColSetTemp = "SetTem";
    private const string ColTempUnit = "TemUn";
    private const string ColRoomTemp = "TemSen";
    private const string ColFanSpeed = "WdSpd";

    // The internal room temperature sensor value has a +40 offset to avoid negative numbers on the wire.
    private const int RoomTempOffset = 40;
    private const int MinTargetTemperature = 16;
    private const int MaxTargetTemperature = 30;

    // Bound device keys don't change across calls (or even process restarts, in practice), so
    // they're cached process-wide to avoid re-binding on every single status/control request.
    private static readonly ConcurrentDictionary<string, BoundGreeDevice> BoundDevices = new();

    private readonly GreeClient _client = new();
    private readonly IPAddress _ipAddress;
    private readonly string _macAddress;

    public AirconditioningService(IConfiguration configuration)
    {
        _ipAddress = IPAddress.Parse(configuration.GetValue<string>("AIRCONDITIONING_IP_ADDRESS"));
        _macAddress = configuration.GetValue<string>("AIRCONDITIONING_MAC_ADDRESS");
    }

    public async Task<AirconditioningOverview> GetOverview()
    {
        var status = await ExecuteWithRebindOnFailure(device =>
            _client.GetStatusAsync(device, _ipAddress, [ColPower, ColMode, ColSetTemp, ColRoomTemp, ColFanSpeed]));

        var isOn = status.GetValueOrDefault(ColPower) == 1;

        return new AirconditioningOverview
        {
            IsOn = isOn,
            RoomTemperature = status.GetValueOrDefault(ColRoomTemp) - RoomTempOffset,
            TargetTemperature = status.GetValueOrDefault(ColSetTemp),
            Mode = isOn ? MapMode(status.GetValueOrDefault(ColMode)) : "Uit",
            FanSpeed = MapFanSpeed(status.GetValueOrDefault(ColFanSpeed))
        };
    }

    public Task<bool> TurnOn() => SetPower(true);

    public Task<bool> TurnOff() => SetPower(false);

    public Task<bool> SetTargetTemperature(int temperature)
    {
        var clamped = Math.Clamp(temperature, MinTargetTemperature, MaxTargetTemperature);

        return ExecuteWithRebindOnFailure(device => _client.SetParametersAsync(device, _ipAddress, new Dictionary<string, int>
        {
            [ColTempUnit] = 0,
            [ColSetTemp] = clamped
        }));
    }

    public async Task<bool> IncreaseTargetTemperature()
    {
        var overview = await GetOverview();
        return await SetTargetTemperature((int)overview.TargetTemperature + 1);
    }

    public async Task<bool> DecreaseTargetTemperature()
    {
        var overview = await GetOverview();
        return await SetTargetTemperature((int)overview.TargetTemperature - 1);
    }

    private Task<bool> SetPower(bool on)
    {
        return ExecuteWithRebindOnFailure(device => _client.SetParametersAsync(device, _ipAddress, new Dictionary<string, int>
        {
            [ColPower] = on ? 1 : 0
        }));
    }

    private static string MapMode(int mode) => mode switch
    {
        1 => "Koelen",
        2 => "Drogen",
        3 => "Ventileren",
        4 => "Verwarmen",
        _ => "Automatisch"
    };

    private static string MapFanSpeed(int fanSpeed) => fanSpeed switch
    {
        1 => "Laag",
        2 => "Medium-laag",
        3 => "Medium",
        4 => "Medium-hoog",
        5 => "Hoog",
        _ => "Automatisch"
    };

    /// <summary>
    /// Runs the given operation against the bound device, using the process-wide cached
    /// binding if available. If the operation fails (e.g. the device's key expired, or it
    /// wasn't bound yet), (re)binds once and retries a single time before giving up.
    /// </summary>
    private async Task<T> ExecuteWithRebindOnFailure<T>(Func<BoundGreeDevice, Task<T>> operation)
    {
        if (BoundDevices.TryGetValue(_macAddress, out var device))
        {
            try
            {
                return await operation(device);
            }
            catch (Exception)
            {
                BoundDevices.TryRemove(_macAddress, out _);
            }
        }

        device = await _client.BindAsync(_macAddress, _ipAddress)
            ?? throw new InvalidOperationException("Could not bind to the air conditioning unit. Check its IP/MAC address and make sure it's reachable on the network.");

        BoundDevices[_macAddress] = device;

        return await operation(device);
    }
}
