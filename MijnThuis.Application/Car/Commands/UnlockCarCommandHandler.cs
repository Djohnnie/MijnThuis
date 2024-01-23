using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Commands;

public class UnlockCarCommandHandler : IRequestHandler<UnlockCarCommand, CarCommandResponse>
{
    private readonly ICarService _carService;

    public UnlockCarCommandHandler(ICarService carService)
    {
        _carService = carService;
    }

    public async Task<CarCommandResponse> Handle(UnlockCarCommand request, CancellationToken cancellationToken)
    {
        var result = await _carService.Unlock();

        return new CarCommandResponse
        {
            Success = result
        };
    }
}