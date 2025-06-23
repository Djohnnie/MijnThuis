namespace MijnThuis.Contracts.Power;

public record GetPowerOverviewResponse
{
    public string Description { get; set; }
    public decimal CurrentPower { get; init; }
    public decimal CurrentConsumption { get; set; }
    public int PowerPeak { get; init; }
    public decimal ImportToday { get; set; }
    public decimal ExportToday { get; set; }
    public decimal ImportThisMonth { get; set; }
    public decimal ExportThisMonth { get; set; }
    public string CurrentPricePeriod { get; set; }
    public decimal CurrentConsumptionPrice { get; set; }
    public decimal CurrentInjectionPrice { get; set; }
    public bool IsTvOn { get; set; }
    public bool IsBureauOn { get; set; }
    public bool IsVijverOn { get; set; }
    public bool IsTheFrameOn { get; set; }
}