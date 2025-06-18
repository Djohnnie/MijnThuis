using MediatR;

namespace MijnThuis.Contracts.Car;

public class CarOverheatProtectionCommand : IRequest<CarCommandResponse>
{
    public bool Enable { get; set; }
}