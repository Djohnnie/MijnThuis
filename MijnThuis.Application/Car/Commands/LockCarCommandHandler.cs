using MediatR;
using Microsoft.Extensions.Configuration;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Commands;

public class LockCarCommandHandler : IRequestHandler<LockCarCommand, CarCommandResponse>
{
    private readonly ICarService _carService;
    private readonly IConfiguration _configuration;

    public LockCarCommandHandler(
        ICarService carService,
        IConfiguration configuration)
    {
        _carService = carService;
        _configuration = configuration;
    }

    public async Task<CarCommandResponse> Handle(LockCarCommand request, CancellationToken cancellationToken)
    {
        var pin = _configuration.GetValue<string>("PINCODE");

        if (request.Pin != pin)
        {
            return new CarCommandResponse
            {
                Success = false
            };
        }

        var lockResult = await _carService.Lock();

        if (!lockResult)
        {
            return new CarCommandResponse
            {
                Success = false
            };
        }

        var isLocked = false;

        while (!isLocked)
        {
            var overviewResult = await _carService.GetOverview();
            isLocked = overviewResult.IsLocked;

            if (!isLocked)
            {
                await Task.Delay(1000);
            }
        }

        return new CarCommandResponse
        {
            Success = lockResult
        };
    }
}