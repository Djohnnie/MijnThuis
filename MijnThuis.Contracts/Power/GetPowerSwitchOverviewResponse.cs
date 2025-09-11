namespace MijnThuis.Contracts.Power;

public class GetPowerSwitchOverviewResponse
{
    public bool IsTvOn { get; set; }
    public bool IsBureauOn { get; set; }
    public bool IsVijverOn { get; set; }
    public bool IsTheFrameOn { get; set; }
}
