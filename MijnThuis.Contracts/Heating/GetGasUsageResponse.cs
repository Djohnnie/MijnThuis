namespace MijnThuis.Contracts.Heating;

public class GetGasUsageResponse
{
    public List<GasUsageEntry> Entries { get; set; }
}

public class GasUsageEntry
{
    public DateTime Date { get; set; }
    public decimal GasAmount { get; set; }
}