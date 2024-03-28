using MediatR;
using MijnThuis.Contracts.Lamps;
using MijnThuis.Integrations.Lamps;

namespace MijnThuis.Application.Lamps.Queries;

public class GetLampsOverviewQueryHandler : IRequestHandler<GetLampsOverviewQuery, GetLampsOverviewResponse>
{
    private readonly ILampsService _lampsService;

    public GetLampsOverviewQueryHandler(
        ILampsService lampsService)
    {
        _lampsService = lampsService;
    }

    public async Task<GetLampsOverviewResponse> Handle(GetLampsOverviewQuery request, CancellationToken cancellationToken)
    {
        var lampsResult = await _lampsService.GetOverview();

        return new GetLampsOverviewResponse();
    }
}