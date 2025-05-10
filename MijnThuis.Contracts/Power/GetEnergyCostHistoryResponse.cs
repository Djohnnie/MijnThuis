namespace MijnThuis.Contracts.Power;

public class GetEnergyCostHistoryResponse
{
    public List<EnergyCostHistoryEntry> Entries { get; set; } = new();
}

public class EnergyCostHistoryEntry
{
    public DateTime Date { get; set; }
    public decimal ImportCost { get; set; }
    public decimal ExportCost { get; set; }
    public decimal TotalCost { get; set; }
}