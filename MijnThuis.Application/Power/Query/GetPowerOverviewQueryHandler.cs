using Mapster;
using MediatR;
using MijnThuis.Contracts.Power;
using MijnThuis.Integrations.Power;

namespace MijnThuis.Application.Power.Query;

public class GetPowerOverviewQueryHandler : IRequestHandler<GetPowerOverviewQuery, GetPowerOverviewResponse>
{
    private readonly IPowerService _powerService;

    public GetPowerOverviewQueryHandler(IPowerService powerService)
    {
        _powerService = powerService;
    }

    public async Task<GetPowerOverviewResponse> Handle(GetPowerOverviewQuery request, CancellationToken cancellationToken)
    {
        var result = await _powerService.GetOverview();

        return result.Adapt<GetPowerOverviewResponse>();
    }
}