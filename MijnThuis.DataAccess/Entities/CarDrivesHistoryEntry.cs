namespace MijnThuis.DataAccess.Entities;

public class CarDrivesHistoryEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public long TessieId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public string StartingLocation { get; set; }
    public string EndingLocation { get; set; }
    public int StartingOdometer { get; set; }
    public int EndingOdometer { get; set; }
    public int StartingBattery { get; set; }
    public int EndingBattery { get; set; }
    public decimal EnergyUsed { get; set; }
    public int RangeUsed { get; set; }
    public int AverageSpeed { get; set; }
    public int MaximumSpeed { get; set; }
    public int Distance { get; set; }
    public decimal AverageInsideTemperature { get; set; }
    public decimal AverageOutsideTemperature { get; set; }
}