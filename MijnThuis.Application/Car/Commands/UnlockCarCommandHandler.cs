using MediatR;
using Microsoft.Extensions.Configuration;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Commands;

public class UnlockCarCommandHandler : IRequestHandler<UnlockCarCommand, CarCommandResponse>
{
    private readonly ICarService _carService;
    private readonly IConfiguration _configuration;

    public UnlockCarCommandHandler(
        ICarService carService,
        IConfiguration configuration)
    {
        _carService = carService;
        _configuration = configuration;
    }

    public async Task<CarCommandResponse> Handle(UnlockCarCommand request, CancellationToken cancellationToken)
    {
        var pin = _configuration.GetValue<string>("PINCODE");

        if (request.Pin != pin)
        {
            return new CarCommandResponse
            {
                Success = false
            };
        }

        var lockResult = await _carService.Unlock();

        if (!lockResult)
        {
            return new CarCommandResponse
            {
                Success = false
            };
        }

        var isUnlocked = false;

        while (!isUnlocked)
        {
            var overviewResult = await _carService.GetOverview();
            isUnlocked = !overviewResult.IsLocked;

            if (!isUnlocked)
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