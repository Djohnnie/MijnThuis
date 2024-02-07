using Mapster;
using MediatR;
using MijnThuis.Contracts.Power;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Power.Query;

public class GetPowerOverviewQueryHandler : IRequestHandler<GetPowerOverviewQuery, GetPowerOverviewResponse>
{
    private readonly IPowerService _powerService;
    private readonly ISolarService _solarService;

    public GetPowerOverviewQueryHandler(
        IPowerService powerService,
        ISolarService solarService)
    {
        _powerService = powerService;
        _solarService = solarService;
    }

    public async Task<GetPowerOverviewResponse> Handle(GetPowerOverviewQuery request, CancellationToken cancellationToken)
    {
        var powerResult = await _powerService.GetOverview();
        var energyTodayResult = await _solarService.GetEnergyToday();
        var energyThisMonthResult = await _solarService.GetEnergyThisMonth();

        var result = powerResult.Adapt<GetPowerOverviewResponse>();
        result.EnergyToday = energyTodayResult.Purchased / 1000M;
        result.EnergyThisMonth = energyThisMonthResult.Purchased / 1000M;

        return result;
    }
}