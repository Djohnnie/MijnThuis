namespace MijnThuis.Contracts.Car;

public record GetCarOverviewResponse
{
    public string State { get; init; }
    public bool IsLocked { get; init; }
    public bool IsCharging { get; init; }
    public bool IsChargingManually { get; set; }
    public bool IsPreconditioning { get; init; }
    public byte BatteryLevel { get; init; }
    public int RemainingRange { get; init; }
    public int TemperatureInside { get; init; }
    public int TemperatureOutside { get; init; }
    public int BatteryHealth { get; set; }
    public string ChargingCurrent { get; set; }
    public int ChargingAmps { get; set; }
    public string ChargingRange { get; set; }
    public string Address { get; set; }
    public string Charger1 { get; set; }
    public bool Charger1Available { get; set; }
    public string Charger2 { get; set; }
    public bool Charger2Available { get; set; }
}