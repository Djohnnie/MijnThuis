namespace MijnThuis.DataAccess.Entities;

public class EnergyForecastEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime Date { get; set; }
    public int EnergyConsumptionInWattHours { get; set; }
    public int SolarEnergyInWattHours { get; set; }
    public int EstimatedBatteryLevel { get; set; }
}