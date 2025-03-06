namespace MijnThuis.DataAccess.Entities;

public class CarChargingEnergyHistoryEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid ChargingSessionId { get; set; }
    public int ChargingAmps { get; set; }
    public TimeSpan ChargingDuration { get; set; }
    public decimal EnergyCharged { get; set; }
}