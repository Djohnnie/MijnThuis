using MediatR;

namespace MijnThuis.Contracts.Power;

public class SetTvPowerSwitchCommand : IRequest<SetPowerSwitchResponse>
{
    public bool IsOn { get; set; }
}