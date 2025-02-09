namespace MijnThuis.DataAccess.Entities;

public class BatteryEnergyHistoryEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime Date { get; set; }
    public decimal RatedEnergy { get; set; }
    public decimal AvailableEnergy { get; set; }
    public decimal StateOfCharge { get; set; }
    public decimal CalculatedStateOfHealth { get; set; }
    public decimal StateOfHealth { get; set; }
    public DateTime DataCollected { get; set; }
}