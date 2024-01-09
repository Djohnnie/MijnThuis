namespace MijnThuis.Contracts.Power;

public record GetPowerOverviewResponse
{
    public int ActivePower { get; init; }
}