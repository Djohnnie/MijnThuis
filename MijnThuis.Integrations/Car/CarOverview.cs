namespace MijnThuis.Integrations.Car;

public class CarOverview
{
    public string State { get; set; }
    public byte BatteryLevel { get; set; }
    public int RemainingRange { get; set; }
    public int TemperatureInside { get; set; }
    public int TemperatureOutside { get; set; }
    public bool IsLocked { get; set; }
    public bool IsCharging { get; set; }
    public bool IsPreconditioning { get; set; }
    public int ChargingAmps { get; set; }
    public int MaxChargingAmps { get; set; }
    public int ChargeLimit { get; set; }
    public bool IsChargePortOpen { get; set; }
    public decimal ChargeEnergyAdded { get; set; }
    public decimal ChargeRangeAdded { get; set; }
}