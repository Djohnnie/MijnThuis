using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Commands;

public class LockCarCommandHandler : IRequestHandler<LockCarCommand, CarCommandResponse>
{
    private readonly ICarService _carService;

    public LockCarCommandHandler(ICarService carService)
    {
        _carService = carService;
    }

    public async Task<CarCommandResponse> Handle(LockCarCommand request, CancellationToken cancellationToken)
    {
        var result = await _carService.Lock();

        return new CarCommandResponse
        {
            Success = result
        };
    }
}