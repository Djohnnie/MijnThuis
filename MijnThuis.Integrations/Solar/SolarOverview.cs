namespace MijnThuis.Integrations.Solar;

public class SolarOverview
{
    public decimal CurrentSolarPower { get; set; }
    public decimal CurrentBatteryPower { get; set; }
    public decimal CurrentGridPower { get; set; }
    public decimal CurrentConsumptionPower { get; set; }
    public decimal BatteryLevel { get; set; }
}