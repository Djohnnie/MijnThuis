namespace MijnThuis.Contracts.Solar;

public record GetSolarOverviewResponse
{
    public decimal CurrentPower { get; init; }
    public decimal LastDayEnergy { get; set; }
    public decimal LastMonthEnergy { get; set; }
    public int BatteryLevel { get; set; }
    public int BatteryHealth { get; set; }
}