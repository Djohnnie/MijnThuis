namespace MijnThuis.Contracts.Solar;

public class GetBatteryEnergyHistoryResponse
{
    public List<BatteryEnergyHistoryEntry> Entries { get; set; }
}

public class BatteryEnergyHistoryEntry
{
    public DateTime Date { get; set; }
    public decimal RatedEnergy { get; set; }
    public decimal AvailableEnergy { get; set; }
    public decimal StateOfCharge { get; set; }
    public decimal CalculatedStateOfHealth { get; set; }
    public decimal StateOfHealth { get; set; }
}