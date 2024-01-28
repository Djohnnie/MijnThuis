using Mapster;
using MediatR;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Solar.Query;

public class GetSolarOverviewQueryHandler : IRequestHandler<GetSolarOverviewQuery, GetSolarOverviewResponse>
{
    private readonly ISolarService _solarService;

    public GetSolarOverviewQueryHandler(ISolarService solarService)
    {
        _solarService = solarService;
    }

    public async Task<GetSolarOverviewResponse> Handle(GetSolarOverviewQuery request, CancellationToken cancellationToken)
    {
        var solarResult = await _solarService.GetOverview();
        var batteryResult = await _solarService.GetBatteryLevel();

        var result = solarResult.Adapt<GetSolarOverviewResponse>();
        result.BatteryLevel = batteryResult.Level;
        result.BatteryHealth = batteryResult.Health;

        return result;
    }
}