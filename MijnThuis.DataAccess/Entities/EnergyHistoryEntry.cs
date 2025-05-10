namespace MijnThuis.DataAccess.Entities;

public class EnergyHistoryEntry
{
    public Guid Id { get; set; }
    public long SysId { get; set; }
    public DateTime Date { get; set; }
    public byte ActiveTarrif { get; set; }
    public decimal TotalImport { get; set; }
    public decimal TotalImportDelta { get; set; }
    public decimal Tarrif1Import { get; set; }
    public decimal Tarrif1ImportDelta { get; set; }
    public decimal Tarrif2Import { get; set; }
    public decimal Tarrif2ImportDelta { get; set; }
    public decimal TotalExport { get; set; }
    public decimal TotalExportDelta { get; set; }
    public decimal Tarrif1Export { get; set; }
    public decimal Tarrif1ExportDelta { get; set; }
    public decimal Tarrif2Export { get; set; }
    public decimal Tarrif2ExportDelta { get; set; }
    public decimal TotalGas { get; set; }
    public decimal TotalGasDelta { get; set; }
    public decimal GasCoefficient { get; set; }
    public decimal TotalGasKwh { get; set; }
    public decimal TotalGasKwhDelta { get; set; }
    public decimal MonthlyPowerPeak { get; set; }
    public decimal CalculatedImportCost { get; set; }
    public decimal CalculatedExportCost { get; set; }
}