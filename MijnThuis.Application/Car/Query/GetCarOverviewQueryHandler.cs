using Mapster;
using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Query;

public class GetCarOverviewQueryHandler : IRequestHandler<GetCarOverviewQuery, GetCarOverviewResponse>
{
    private readonly ICarService _carService;

    public GetCarOverviewQueryHandler(ICarService carService)
    {
        _carService = carService;
    }

    public async Task<GetCarOverviewResponse> Handle(GetCarOverviewQuery request, CancellationToken cancellationToken)
    {
        var result = await _carService.GetOverview();

        return result.Adapt<GetCarOverviewResponse>();
    }
}