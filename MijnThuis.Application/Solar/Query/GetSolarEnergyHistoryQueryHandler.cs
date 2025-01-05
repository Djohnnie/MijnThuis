using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Query;

public class GetSolarEnergyHistoryQueryHandler : IRequestHandler<GetSolarEnergyHistoryQuery, GetSolarEnergyHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetSolarEnergyHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetSolarEnergyHistoryResponse> Handle(GetSolarEnergyHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.SolarEnergyHistory
            .Where(x => x.Date.Date >= request.From && x.Date.Date <= request.To)
            .ToListAsync();

        var response = new GetSolarEnergyHistoryResponse();

        switch(request.Unit)
        {
            case EnergyHistoryUnit.Day:
                response.Entries = GroupEntriesByDay(entries);
                break;
            case EnergyHistoryUnit.Month:
                response.Entries = GroupEntriesByMonth(entries);
                break;
            case EnergyHistoryUnit.Year:
                response.Entries = GroupEntriesByYear(entries);
                break;
        }

        return response;
    }

    private List<SolarEnergyHistoryEntry> GroupEntriesByDay(List<DataAccess.Entities.SolarEnergyHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.Date.Date)
            .Select(g => new SolarEnergyHistoryEntry
            {
                Date = g.Key,
                Import = g.Sum(x => x.Import) / 1000M,
                Export = g.Sum(x => x.Export) / 1000M,
                Production = g.Sum(x => x.Production) / 1000M,
                ProductionToHome = g.Sum(x => x.ProductionToHome) / 1000M,
                ProductionToBattery = g.Sum(x => x.ProductionToBattery) / 1000M,
                ProductionToGrid = g.Sum(x => x.ProductionToGrid) / 1000M,
                Consumption = g.Sum(x => x.Consumption) / 1000M,
                ConsumptionFromBattery = g.Sum(x => x.ConsumptionFromBattery) / 1000M,
                ConsumptionFromSolar = g.Sum(x => x.ConsumptionFromSolar) / 1000M,
                ConsumptionFromGrid = g.Sum(x => x.ConsumptionFromGrid) / 1000M
            })
            .ToList();
    }

    private List<SolarEnergyHistoryEntry> GroupEntriesByMonth(List<DataAccess.Entities.SolarEnergyHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new SolarEnergyHistoryEntry
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                Import = g.Sum(x => x.Import) / 1000M,
                Export = g.Sum(x => x.Export) / 1000M,
                Production = g.Sum(x => x.Production) / 1000M,
                ProductionToHome = g.Sum(x => x.ProductionToHome) / 1000M,
                ProductionToBattery = g.Sum(x => x.ProductionToBattery) / 1000M,
                ProductionToGrid = g.Sum(x => x.ProductionToGrid) / 1000M,
                Consumption = g.Sum(x => x.Consumption) / 1000M,
                ConsumptionFromBattery = g.Sum(x => x.ConsumptionFromBattery) / 1000M,
                ConsumptionFromSolar = g.Sum(x => x.ConsumptionFromSolar) / 1000M,
                ConsumptionFromGrid = g.Sum(x => x.ConsumptionFromGrid) / 1000M
            })
            .ToList();
    }

    private List<SolarEnergyHistoryEntry> GroupEntriesByYear(List<DataAccess.Entities.SolarEnergyHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.Date.Year)
            .Select(g => new SolarEnergyHistoryEntry
            {
                Date = new DateTime(g.Key, 1, 1),
                Import = g.Sum(x => x.Import) / 1000M,
                Export = g.Sum(x => x.Export) / 1000M,
                Production = g.Sum(x => x.Production) / 1000M,
                ProductionToHome = g.Sum(x => x.ProductionToHome) / 1000M,
                ProductionToBattery = g.Sum(x => x.ProductionToBattery) / 1000M,
                ProductionToGrid = g.Sum(x => x.ProductionToGrid) / 1000M,
                Consumption = g.Sum(x => x.Consumption) / 1000M,
                ConsumptionFromBattery = g.Sum(x => x.ConsumptionFromBattery) / 1000M,
                ConsumptionFromSolar = g.Sum(x => x.ConsumptionFromSolar) / 1000M,
                ConsumptionFromGrid = g.Sum(x => x.ConsumptionFromGrid) / 1000M
            })
            .ToList();
    }
}