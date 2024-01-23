using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Commands;

public class PreheatCarCommandHandler : IRequestHandler<PreheatCarCommand, CarCommandResponse>
{
    private readonly ICarService _carService;

    public PreheatCarCommandHandler(ICarService carService)
    {
        _carService = carService;
    }

    public async Task<CarCommandResponse> Handle(PreheatCarCommand request, CancellationToken cancellationToken)
    {
        var result = await _carService.Preheat();

        return new CarCommandResponse
        {
            Success = result
        };
    }
}