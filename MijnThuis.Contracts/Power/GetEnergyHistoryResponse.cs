namespace MijnThuis.Contracts.Power;

public class GetEnergyHistoryResponse
{
    public List<EnergyHistoryEntry> Entries { get; set; }
}

public class EnergyHistoryEntry
{
    public DateTime Date { get; set; }
    public decimal TotalImport { get; set; }
    public decimal Tarrif1Import { get; set; }
    public decimal Tarrif2Import { get; set; }
    public decimal TotalExport { get; set; }
    public decimal Tarrif1Export { get; set; }
    public decimal Tarrif2Export { get; set; }
    public decimal TotalGas { get; set; }
    public decimal TotalGasKwh { get; set; }
    public decimal MonthlyPowerPeak { get; set; }
}