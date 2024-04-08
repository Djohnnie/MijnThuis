using MediatR;

namespace MijnThuis.Contracts.Power;

public class SetVijverPowerSwitchCommand : IRequest<SetPowerSwitchResponse>
{
    public bool IsOn { get; set; }
}