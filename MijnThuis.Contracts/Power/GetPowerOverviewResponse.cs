namespace MijnThuis.Contracts.Power;

public record GetPowerOverviewResponse
{
    public int CurrentPower { get; init; }
    public int PowerPeak { get; init; }
}