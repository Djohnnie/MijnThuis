namespace MijnThuis.Contracts.Solar;

public record GetSolarOverviewResponse
{
    public int CurrentPower { get; init; }
    public int BatteryLevel { get; init; }
}