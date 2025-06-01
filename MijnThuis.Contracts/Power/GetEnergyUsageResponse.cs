namespace MijnThuis.Contracts.Power;

public class GetEnergyUsageResponse
{
    public List<PowerUsageEntry> Entries { get; set; }
}

public class PowerUsageEntry
{
    public DateTime Date { get; set; }
    public decimal EnergyImport { get; set; }
    public decimal EnergyExport { get; set; }
}