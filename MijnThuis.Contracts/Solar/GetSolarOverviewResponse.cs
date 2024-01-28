namespace MijnThuis.Contracts.Solar;

public record GetSolarOverviewResponse
{
    public decimal CurrentPower { get; init; }
    public decimal LastDayEnergy { get; init; }
    public decimal LastMonthEnergy { get; init; }
    public decimal BatteryLevel { get; set; }
    public decimal BatteryHealth { get; set; }
}