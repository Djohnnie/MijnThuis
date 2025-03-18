namespace MijnThuis.Contracts.SmartLock;

public class GetSmartLockOverviewResponse
{
    public string State { get; set; }
    public string DoorState { get; set; }
    public int BatteryCharge { get; set; }
    public List<SmartLockActivityLogEntry> ActivityLog { get; set; }
}

public class SmartLockActivityLogEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; }
}