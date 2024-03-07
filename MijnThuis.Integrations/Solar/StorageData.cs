namespace MijnThuis.Integrations.Solar;

public enum StorageDataRange
{
    Today,
    ThreeDays,
    Week,
    Month
}

public class StorageData
{
    public List<StorageDataEntry> Entries { get; set; }
}

public class StorageDataEntry
{
    //public string Timestamp { get; set; }
    public decimal ChargeState { get; set; }
}