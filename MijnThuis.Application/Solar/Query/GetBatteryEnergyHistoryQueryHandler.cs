using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Query;

public class GetBatteryEnergyHistoryQueryHandler : IRequestHandler<GetBatteryEnergyHistoryQuery, GetBatteryEnergyHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetBatteryEnergyHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetBatteryEnergyHistoryResponse> Handle(GetBatteryEnergyHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.BatteryEnergyHistory
            .Where(x => x.Date.Date >= request.From && x.Date.Date <= request.To)
            .ToListAsync();

        var response = new GetBatteryEnergyHistoryResponse();

        switch (request.Unit)
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

    private List<BatteryEnergyHistoryEntry> GroupEntriesByDay(List<DataAccess.Entities.BatteryEnergyHistoryEntry> entries)
    {
        return entries
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
            .ToList();
    }

    private List<BatteryEnergyHistoryEntry> GroupEntriesByMonth(List<DataAccess.Entities.BatteryEnergyHistoryEntry> entries)
    {
        return entries
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
            .ToList();
    }

    private List<BatteryEnergyHistoryEntry> GroupEntriesByYear(List<DataAccess.Entities.BatteryEnergyHistoryEntry> entries)
    {
        return entries
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
            .ToList();
    }
}