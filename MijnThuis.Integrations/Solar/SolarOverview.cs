namespace MijnThuis.Integrations.Solar;

public class SolarOverview
{
    public decimal CurrentPower { get; set; }
    public decimal BatteryLevel { get; set; }
    public decimal LastDayEnergy { get; init; }
    public decimal LastMonthEnergy { get; init; }
}