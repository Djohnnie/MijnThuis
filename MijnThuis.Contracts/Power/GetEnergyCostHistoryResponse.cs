namespace MijnThuis.Contracts.Power;

public class GetEnergyCostHistoryResponse
{
    public List<EnergyCostHistoryEntry> Entries { get; set; } = new();
}

public class EnergyCostHistoryEntry
{
    public DateTime Date { get; set; }
    public decimal ImportEnergy { get; set; }
    public decimal ExportEnergy { get; set; }
    public decimal EnergyCost { get; set; }
    public decimal CapacityTariffCost { get; set; }
    public decimal TransportCost { get; set; }
    public decimal Taxes { get; set; }
}