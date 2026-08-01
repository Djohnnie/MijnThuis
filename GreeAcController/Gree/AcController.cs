namespace GreeAcController.Gree;

/// <summary>
/// High-level convenience wrapper around <see cref="GreeClient"/> exposing the
/// specific operations needed by this prototype: power on/off, target temperature
/// and current room temperature.
/// </summary>
public class AcController
{
    private const string ColPower = "Pow";
    private const string ColSetTemp = "SetTem";
    private const string ColTempUnit = "TemUn";
    private const string ColRoomTemp = "TemSen";

    // The internal sensor value has a +40 offset to avoid negative numbers on the wire.
    private const int RoomTempOffset = 40;

    private readonly GreeClient _client;
    private readonly GreeDevice _device;

    public AcController(GreeClient client, GreeDevice device)
    {
        _client = client;
        _device = device;
    }

    public async Task<bool> IsPoweredOnAsync(CancellationToken cancellationToken = default)
    {
        var status = await _client.GetStatusAsync(_device, [ColPower], cancellationToken);
        return status.TryGetValue(ColPower, out var value) && value == 1;
    }

    public Task<bool> TurnOnAsync(CancellationToken cancellationToken = default) =>
        _client.SetParametersAsync(_device, new Dictionary<string, int> { [ColPower] = 1 }, cancellationToken);

    public Task<bool> TurnOffAsync(CancellationToken cancellationToken = default) =>
        _client.SetParametersAsync(_device, new Dictionary<string, int> { [ColPower] = 0 }, cancellationToken);

    /// <summary>
    /// Sets the target temperature in Celsius (typical unit range is 16-30 &#176;C).
    /// </summary>
    public Task<bool> SetTargetTemperatureAsync(int celsius, CancellationToken cancellationToken = default) =>
        _client.SetParametersAsync(_device, new Dictionary<string, int>
        {
            [ColTempUnit] = 0,
            [ColSetTemp] = celsius
        }, cancellationToken);

    public async Task<int> GetTargetTemperatureAsync(CancellationToken cancellationToken = default)
    {
        var status = await _client.GetStatusAsync(_device, [ColSetTemp, ColTempUnit], cancellationToken);
        return status.GetValueOrDefault(ColSetTemp);
    }

    /// <summary>
    /// Reads the current room temperature from the unit's internal sensor, in Celsius.
    /// Returns null if the device doesn't report a valid sensor value.
    /// </summary>
    public async Task<double?> GetRoomTemperatureAsync(CancellationToken cancellationToken = default)
    {
        var status = await _client.GetStatusAsync(_device, [ColRoomTemp], cancellationToken);
        if (!status.TryGetValue(ColRoomTemp, out var raw) || raw <= 0)
        {
            return null;
        }

        return raw - RoomTempOffset;
    }
}
