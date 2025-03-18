namespace MijnThuis.Integrations.SmartLock;

public class SmartLockLog
{
    public DateTime Timestamp { get; set; }
    public SmartLockAction Action { get; set; }
}

public enum SmartLockAction
{
    Unlock = 1,
    Lock = 2,
    Unlatch = 3,
    DoorOpened = 240,
    DoorClosed = 241
}