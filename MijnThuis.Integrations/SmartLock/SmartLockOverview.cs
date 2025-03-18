namespace MijnThuis.Integrations.SmartLock;

public class SmartLockOverview
{
    public SmartLockState State { get; set; }
    public SmartLockDoorState DoorState { get; set; }
    public int BatteryCharge { get; set; }
}

public enum SmartLockState
{
    Locked = 1,
    Unlocking = 2,
    Unlocked = 3,
    Locking = 4,
}

public enum SmartLockDoorState
{
    DoorClosed = 2,
    DoorOpened = 3,
}