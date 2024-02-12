using MediatR;

namespace MijnThuis.Contracts.Heating;

public class SetAntiFrostHeatingCommand : IRequest<HeatingCommandResponse>
{
}