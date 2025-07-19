using MediatR;

namespace MijnThuis.Contracts.Car;

public class PreheatCarCommand : IRequest<CarCommandResponse>
{
    public string Pin { get; set; }
}