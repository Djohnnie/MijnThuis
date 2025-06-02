namespace MijnThuis.DataAccess.Entities;

public class CarChargesHistoryEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public long TessieId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public string Location { get; set; }
    public string LocationFriendlyName { get; set; }
    public bool IsSupercharger { get; set; }
    public bool IsFastCharger { get; set; }
    public int Odometer { get; set; }
    public decimal EnergyAdded { get; set; }
    public decimal EnergyUsed { get; set; }
    public int RangeAdded { get; set; }
    public int BatteryLevelStart { get; set; }
    public int BatteryLevelEnd { get; set; }
    public int DistanceSinceLastCharge { get; set; }
}