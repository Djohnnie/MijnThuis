namespace MijnThuis.Contracts.Car;

public record GetCarOverviewResponse
{
    public string State { get; init; }
    public byte BatteryLevel { get; init; }
    public int RemainingRange { get; init; }
    public string Location { get; init; }
}