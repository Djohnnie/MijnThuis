namespace MijnThuis.Contracts.Solar;

public record GetSolarOverviewResponse
{
    public decimal CurrentSolarPower { get; init; }
    public decimal CurrentBatteryPower { get; init; }
    public decimal CurrentGridPower { get; init; }
    public decimal LastDayEnergy { get; set; }
    public decimal LastMonthEnergy { get; set; }
    public int BatteryLevel { get; set; }
    public int BatteryHealth { get; set; }
}