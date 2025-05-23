using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Queries;

public class GetSolarProductionAndConsumptionTodayQuery : IRequest<GetSolarProductionAndConsumptionTodayResponse>
{
}

public class GetSolarProductionAndConsumptionTodayResponse
{
    public decimal ProductionToHome { get; set; }
    public decimal ProductionToBattery { get; set; }
    public decimal ProductionToGrid { get; set; }
    public decimal ConsumptionFromSolar { get; set; }
    public decimal ConsumptionFromBattery { get; set; }
    public decimal ConsumptionFromGrid { get; set; }
}

public class GetSolarProductionAndConsumptionTodayQueryHandler : IRequestHandler<GetSolarProductionAndConsumptionTodayQuery, GetSolarProductionAndConsumptionTodayResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetSolarProductionAndConsumptionTodayQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetSolarProductionAndConsumptionTodayResponse> Handle(GetSolarProductionAndConsumptionTodayQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;

        var entries = await _dbContext.SolarEnergyHistory
            .Where(x => x.Date.Date == today)
            .OrderBy(x => x.Date)
            .ToListAsync();

        return new GetSolarProductionAndConsumptionTodayResponse
        {
            ProductionToHome = entries.Sum(x => x.ProductionToHome) / 1000M,
            ProductionToBattery = entries.Sum(x => x.ProductionToBattery) / 1000M,
            ProductionToGrid = entries.Sum(x => x.ProductionToGrid) / 1000M,
            ConsumptionFromSolar = entries.Sum(x => x.ConsumptionFromSolar) / 1000M,
            ConsumptionFromBattery = entries.Sum(x => x.ConsumptionFromBattery) / 1000M,
            ConsumptionFromGrid = entries.Sum(x => x.ConsumptionFromGrid) / 1000M
        };
    }
}