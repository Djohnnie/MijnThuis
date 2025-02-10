namespace MijnThuis.Integrations.Power;

public class PowerOverview
{
    public byte ActiveTarrif { get; set; }
    public decimal TotalImport { get; set; }
    public decimal Tarrif1Import { get; set; }
    public decimal Tarrif2Import { get; set; }
    public decimal TotalExport { get; set; }
    public decimal Tarrif1Export { get; set; }
    public decimal Tarrif2Export { get; set; }
    public decimal TotalGas { get; set; }
    public int CurrentPower { get; set; }
    public int PowerPeak { get; set; }
}