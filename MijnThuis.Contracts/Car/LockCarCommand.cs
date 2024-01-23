using MediatR;

namespace MijnThuis.Contracts.Car;

public class LockCarCommand : IRequest<CarCommandResponse>
{
}