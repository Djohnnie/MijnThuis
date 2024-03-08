using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Car;

public record CarOverview
{
    public string State { get; init; }
    public byte BatteryLevel { get; init; }
    public int RemainingRange { get; init; }
    public int TemperatureInside { get; init; }
    public int TemperatureOutside { get; init; }
    public bool IsLocked { get; init; }
    public bool IsCharging { get; init; }
    public bool IsPreconditioning { get; init; }
    public int ChargingAmps { get; init; }
    public int MaxChargingAmps { get; init; }
    public bool IsChargePortOpen { get; init; }
    public decimal ChargeEnergyAdded { get; init; }
    public decimal ChargeRangeAdded { get; init; }
}