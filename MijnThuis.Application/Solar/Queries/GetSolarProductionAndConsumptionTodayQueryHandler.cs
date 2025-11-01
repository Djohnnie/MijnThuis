using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Queries;

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
            Production = entries.Sum(x => x.Production) / 1000M,
            ProductionToHome = entries.Sum(x => x.ProductionToHome) / 1000M,
            ProductionToBattery = entries.Sum(x => x.ProductionToBattery) / 1000M,
            ProductionToGrid = entries.Sum(x => x.ProductionToGrid) / 1000M,
            Consumption = (entries.Sum(x => x.Consumption) + entries.Sum(x => x.ImportToBattery) - entries.Sum(x => x.ConsumptionFromBattery)) / 1000M,
            ConsumptionFromSolar = entries.Sum(x => x.ConsumptionFromSolar) / 1000M,
            ConsumptionFromBattery = entries.Sum(x => x.ConsumptionFromBattery) / 1000M,
            ConsumptionFromGrid = (entries.Sum(x => x.ConsumptionFromGrid) + entries.Sum(x => x.ImportToBattery)) / 1000M
        };
    }
}