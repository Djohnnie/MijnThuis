namespace MijnThuis.Contracts.Car;

public record GetCarOverviewResponse
{
    public string State { get; init; }
    public bool IsLocked { get; init; }
    public bool IsCharging { get; init; }
    public bool IsPreconditioning { get; init; }
    public byte BatteryLevel { get; init; }
    public int RemainingRange { get; init; }
    public int TemperatureInside { get; init; }
    public int TemperatureOutside { get; init; }
    public string Location { get; init; }
}