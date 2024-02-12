using MediatR;

namespace MijnThuis.Contracts.Power;

public class SetBureauPowerSwitchCommand : IRequest<SetPowerSwitchResponse>
{
    public bool IsOn { get; set; }
}