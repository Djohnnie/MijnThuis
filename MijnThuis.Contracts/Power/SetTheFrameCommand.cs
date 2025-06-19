using MediatR;

namespace MijnThuis.Contracts.Power;

public class SetTheFrameCommand : IRequest<SetTheFrameResponse>
{
    public bool TurnOn { get; set; }
}