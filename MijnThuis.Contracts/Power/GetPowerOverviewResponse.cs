namespace MijnThuis.Contracts.Power;

public record GetPowerOverviewResponse
{
    public int CurrentPower { get; init; }
    public int PowerPeak { get; init; }
    public decimal EnergyToday { get; set; }
    public decimal EnergyThisMonth { get; set; }
}