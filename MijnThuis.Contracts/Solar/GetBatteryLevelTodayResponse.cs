namespace MijnThuis.Contracts.Solar;

public class GetBatteryLevelTodayResponse
{
    public List<BatteryLevelEntry> Entries { get; set; } = new();
}

public class BatteryLevelEntry
{
    public DateTime Date { get; set; }
    public int? LevelOfCharge { get; set; }
    public int? StateOfHealth { get; set; }
}