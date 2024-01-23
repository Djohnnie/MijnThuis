using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Commands;

public class CarFartCommandHandler : IRequestHandler<CarFartCommand, CarCommandResponse>
{
    private readonly ICarService _carService;

    public CarFartCommandHandler(ICarService carService)
    {
        _carService = carService;
    }

    public async Task<CarCommandResponse> Handle(CarFartCommand request, CancellationToken cancellationToken)
    {
        var result = await _carService.Fart();

        return new CarCommandResponse
        {
            Success = result
        };
    }
}