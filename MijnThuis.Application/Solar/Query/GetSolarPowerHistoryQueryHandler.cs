using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Query;

public class GetSolarPowerHistoryQueryHandler : IRequestHandler<GetSolarPowerHistoryQuery, GetSolarPowerHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetSolarPowerHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetSolarPowerHistoryResponse> Handle(GetSolarPowerHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.SolarPowerHistory
            .Where(x => x.Date.Date >= request.From && x.Date.Date <= request.To)
            .ToListAsync();

        var response = new GetSolarPowerHistoryResponse();

        switch (request.Unit)
        {
            case PowerHistoryUnit.FifteenMinutes:
                response.Entries = MapEntries(entries);
                break;
            case PowerHistoryUnit.Day:
                response.Entries = GroupEntriesByDay(entries);
                break;
            case PowerHistoryUnit.Month:
                response.Entries = GroupEntriesByMonth(entries);
                break;
            case PowerHistoryUnit.Year:
                response.Entries = GroupEntriesByYear(entries);
                break;
        }

        return response;
    }

    private List<SolarPowerHistoryEntry> MapEntries(List<DataAccess.Entities.SolarPowerHistoryEntry> entries)
    {
        return entries
            .Select(x => new SolarPowerHistoryEntry
            {
                Date = x.Date,
                Import = x.Import,
                Export = x.Export,
                Production = x.Production,
                ProductionToHome = x.ProductionToHome,
                ProductionToBattery = x.ProductionToBattery,
                ProductionToGrid = x.ProductionToGrid,
                Consumption = x.Consumption,
                ConsumptionFromBattery = x.ConsumptionFromBattery,
                ConsumptionFromSolar = x.ConsumptionFromSolar,
                ConsumptionFromGrid = x.ConsumptionFromGrid,
                StorageLevel = x.StorageLevel * 100M
            })
            .ToList();
    }

    private List<SolarPowerHistoryEntry> GroupEntriesByDay(List<DataAccess.Entities.SolarPowerHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.Date.Date)
            .Select(g => new SolarPowerHistoryEntry
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

    private List<SolarPowerHistoryEntry> GroupEntriesByMonth(List<DataAccess.Entities.SolarPowerHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new SolarPowerHistoryEntry
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

    private List<SolarPowerHistoryEntry> GroupEntriesByYear(List<DataAccess.Entities.SolarPowerHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.Date.Year)
            .Select(g => new SolarPowerHistoryEntry
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