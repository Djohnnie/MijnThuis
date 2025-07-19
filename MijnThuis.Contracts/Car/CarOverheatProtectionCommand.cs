using MediatR;

namespace MijnThuis.Contracts.Car;

public class CarOverheatProtectionCommand : IRequest<CarCommandResponse>
{
    public string Pin { get; set; }
    public bool Enable { get; set; }
}