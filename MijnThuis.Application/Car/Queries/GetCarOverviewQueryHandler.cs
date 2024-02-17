using Mapster;
using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Queries;

public class GetCarOverviewQueryHandler : IRequestHandler<GetCarOverviewQuery, GetCarOverviewResponse>
{
    private readonly ICarService _carService;

    public GetCarOverviewQueryHandler(ICarService carService)
    {
        _carService = carService;
    }

    public async Task<GetCarOverviewResponse> Handle(GetCarOverviewQuery request, CancellationToken cancellationToken)
    {
        var overviewResult = await _carService.GetOverview();
        var batteryResult = await _carService.GetBatteryHealth();
        var locationResult = await _carService.GetLocation();

        var result = overviewResult.Adapt<GetCarOverviewResponse>();
        result.BatteryHealth = (int)Math.Round(batteryResult.Percentage);
        result.Address = string.Join(", ", locationResult.Address.Split(", ").Take(2));

        return result;
    }
}