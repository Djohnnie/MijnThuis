using MediatR;
using MijnThuis.Contracts.Power;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Samsung;

namespace MijnThuis.Application.Power.Queries;

public class GetPowerSwitchOverviewQueryHandler : IRequestHandler<GetPowerSwitchOverviewQuery, GetPowerSwitchOverviewResponse>
{
    private readonly IShellyService _shellyService;
    private readonly ISamsungService _samsungService;

    public GetPowerSwitchOverviewQueryHandler(
        IShellyService shellyService,
        ISamsungService samsungService)
    {
        _shellyService = shellyService;
        _samsungService = samsungService;
    }

    public async Task<GetPowerSwitchOverviewResponse> Handle(GetPowerSwitchOverviewQuery request, CancellationToken cancellationToken)
    {        
        var tvPowerSwitchOverview = await _shellyService.GetTvPowerSwitchOverview();
        var bureauPowerSwitchOverview = await _shellyService.GetBureauPowerSwitchOverview();
        var vijverPowerSwitchOverview = await _shellyService.GetVijverPowerSwitchOverview();

        var result = new GetPowerSwitchOverviewResponse();
        result.IsTvOn = tvPowerSwitchOverview.IsOn;
        result.IsBureauOn = bureauPowerSwitchOverview.IsOn;
        result.IsVijverOn = vijverPowerSwitchOverview.IsOn;
        result.IsTheFrameOn = await _samsungService.IsTheFrameOn();

        return result;
    }
}