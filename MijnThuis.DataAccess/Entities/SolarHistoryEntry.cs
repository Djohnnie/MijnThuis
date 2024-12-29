namespace MijnThuis.DataAccess.Entities;

public class SolarHistoryEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime Date { get; set; }
    public decimal Import { get; set; }
    public decimal Export { get; set; }
    public decimal Production { get; set; }
    public decimal ProductionToHome { get; set; }
    public decimal ProductionToBattery { get; set; }
    public decimal ProductionToGrid { get; set; }
    public decimal Consumption { get; set; }
    public decimal ConsumptionFromBattery { get; set; }
    public decimal ConsumptionFromSolar { get; set; }
    public decimal ConsumptionFromGrid { get; set; }
    public DateTime DataCollected { get; set; }
}