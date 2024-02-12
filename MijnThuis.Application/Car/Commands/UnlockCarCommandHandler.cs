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