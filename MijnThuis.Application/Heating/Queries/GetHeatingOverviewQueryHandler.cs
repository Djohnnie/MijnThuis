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
        var heatingResult = await _heatingService.GetOverview();

        var result = heatingResult.Adapt<GetHeatingOverviewResponse>();
        result.Mode = heatingResult.Mode switch
        {
            "Preheat" => "Voorverwarmen",
            "Manual" => "Handmatig",
            "Scheduling" => "Schema",
            "FrostProtection" => "Uit/Vorstbeveiliging",
            "TemporaryOverride" => "Tijdelijke overname",
            _ => "Uit"
        };
        return result;
    }
}