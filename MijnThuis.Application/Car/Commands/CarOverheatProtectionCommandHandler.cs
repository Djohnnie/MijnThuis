using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Commands;

public class CarOverheatProtectionCommandHandler : IRequestHandler<CarOverheatProtectionCommand, CarCommandResponse>
{
    private readonly ICarService _carService;

    public CarOverheatProtectionCommandHandler(ICarService carService)
    {
        _carService = carService;
    }

    public async Task<CarCommandResponse> Handle(CarOverheatProtectionCommand request, CancellationToken cancellationToken)
    {
        var result = await _carService.SetCabinHeatProtection(request.Enable, fanOnly: false);

        return new CarCommandResponse
        {
            Success = result
        };
    }
}