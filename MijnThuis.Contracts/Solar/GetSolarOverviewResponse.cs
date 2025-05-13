namespace MijnThuis.Contracts.Solar;

public record GetSolarOverviewResponse
{
    public decimal CurrentSolarPower { get; set; }
    public decimal CurrentBatteryPower { get; set; }
    public decimal CurrentGridPower { get; set; }
    public decimal LastDayEnergy { get; set; }
    public decimal LastMonthEnergy { get; set; }
    public decimal SolarForecastToday { get; set; }
    public decimal SolarForecastTomorrow { get; set; }
    public int BatteryLevel { get; set; }
    public int BatteryHealth { get; set; }
    public int BatteryMaxEnergy { get; set; }
}