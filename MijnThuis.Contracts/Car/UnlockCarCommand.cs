using MediatR;

namespace MijnThuis.Contracts.Car;

public class UnlockCarCommand : IRequest<CarCommandResponse>
{
    public string Pin { get; set; }
}