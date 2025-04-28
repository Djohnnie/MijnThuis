using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Queries;

public class GetBatteryEnergyHistoryQueryHandler : IRequestHandler<GetBatteryEnergyHistoryQuery, GetBatteryEnergyHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetBatteryEnergyHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetBatteryEnergyHistoryResponse> Handle(GetBatteryEnergyHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = _dbContext.BatteryEnergyHistory
            .Where(x => x.Date.Date >= request.From && x.Date.Date <= request.To);

        var response = new GetBatteryEnergyHistoryResponse();

        switch (request.Unit)
        {
            case EnergyHistoryUnit.Day:
                response.Entries = await GroupEntriesByDay(entries);
                break;
            case EnergyHistoryUnit.Month:
                response.Entries = await GroupEntriesByMonth(entries);
                break;
            case EnergyHistoryUnit.Year:
                response.Entries = await GroupEntriesByYear(entries);
                break;
        }

        return response;
    }

    private async Task<List<BatteryEnergyHistoryEntry>> GroupEntriesByDay(IQueryable<DataAccess.Entities.BatteryEnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Date)
            .Select(g => new BatteryEnergyHistoryEntry
            {
                Date = g.Key,
                RatedEnergy = g.Average(x => x.RatedEnergy),
                AvailableEnergy = g.Average(x => x.AvailableEnergy),
                StateOfCharge = g.Average(x => x.StateOfCharge),
                CalculatedStateOfHealth = g.Average(x => x.CalculatedStateOfHealth),
                StateOfHealth = g.Average(x => x.StateOfHealth)
            })
            .ToListAsync();
    }

    private async Task<List<BatteryEnergyHistoryEntry>> GroupEntriesByMonth(IQueryable<DataAccess.Entities.BatteryEnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new BatteryEnergyHistoryEntry
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                RatedEnergy = g.Average(x => x.RatedEnergy),
                AvailableEnergy = g.Average(x => x.AvailableEnergy),
                StateOfCharge = g.Average(x => x.StateOfCharge),
                CalculatedStateOfHealth = g.Average(x => x.CalculatedStateOfHealth),
                StateOfHealth = g.Average(x => x.StateOfHealth)
            })
            .ToListAsync();
    }

    private async Task<List<BatteryEnergyHistoryEntry>> GroupEntriesByYear(IQueryable<DataAccess.Entities.BatteryEnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Year)
            .Select(g => new BatteryEnergyHistoryEntry
            {
                Date = new DateTime(g.Key, 1, 1),
                RatedEnergy = g.Average(x => x.RatedEnergy),
                AvailableEnergy = g.Average(x => x.AvailableEnergy),
                StateOfCharge = g.Average(x => x.StateOfCharge),
                CalculatedStateOfHealth = g.Average(x => x.CalculatedStateOfHealth),
                StateOfHealth = g.Average(x => x.StateOfHealth)
            })
            .ToListAsync();
    }
}