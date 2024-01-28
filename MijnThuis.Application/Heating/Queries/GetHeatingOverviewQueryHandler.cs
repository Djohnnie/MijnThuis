using Mapster;
using MediatR;
using MijnThuis.Contracts.Heating;
using MijnThuis.Integrations.Heating;

namespace MijnThuis.Application.Heating.Queries;

public class GetHeatingOverviewQueryHandler : IRequestHandler<GetHeatingOverviewQuery, GetHeatingOverviewResponse>
{
    private readonly IHeatingService _heatingService;

    public GetHeatingOverviewQueryHandler(IHeatingService heatingService)
    {
        _heatingService = heatingService;
    }

    public async Task<GetHeatingOverviewResponse> Handle(GetHeatingOverviewQuery request, CancellationToken cancellationToken)
    {
        var result = await _heatingService.GetOverview();

        return result.Adapt<GetHeatingOverviewResponse>();
    }
}