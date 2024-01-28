using MediatR;
using MijnThuis.Contracts.Sauna;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Sauna;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Sauna.Query;

public class GetSaunaOverviewQueryHandler : IRequestHandler<GetSaunaOverviewQuery, GetSaunaOverviewResponse>
{
    private readonly ISaunaService _saunaService;

    public GetSaunaOverviewQueryHandler(ISaunaService saunaService)
    {
        _saunaService = saunaService;
    }

    public async Task<GetSaunaOverviewResponse> Handle(GetSaunaOverviewQuery request, CancellationToken cancellationToken)
    {
        var state = await _saunaService.GetState();
        var insideTemperature = await _saunaService.GetInsideTemperature();
        var outsideTemperature = await _saunaService.GetOutsideTemperature();
        var power = await _saunaService.GetPowerUsage();

        return new GetSaunaOverviewResponse
        {
            State = state,
            InsideTemperature = insideTemperature,
            OutsideTemperature = outsideTemperature,
            Power = power
        };
    }
}