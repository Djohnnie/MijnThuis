using MediatR;

namespace MijnThuis.Contracts.Heating;

public class SetScheduledHeatingCommand : IRequest<HeatingCommandResponse>
{
}