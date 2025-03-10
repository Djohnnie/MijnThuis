using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Car;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Car.Queries;

public class GetCarChargingHistoryQueryHandler : IRequestHandler<GetCarChargingHistoryQuery, GetCarChargingHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetCarChargingHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetCarChargingHistoryResponse> Handle(GetCarChargingHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.CarChargingHistory
            .Where(x => x.Timestamp.Date >= request.From && x.Timestamp.Date <= request.To)
            .ToListAsync();

        var response = new GetCarChargingHistoryResponse();

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

    private List<CarChargingEnergyHistoryEntry> GroupEntriesByDay(List<DataAccess.Entities.CarChargingEnergyHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.Timestamp.Date)
            .Select(g => new CarChargingEnergyHistoryEntry
            {
                Date = g.Key,
                EnergyCharged = g.Sum(x => x.EnergyCharged)
            })
            .ToList();
    }

    private List<CarChargingEnergyHistoryEntry> GroupEntriesByMonth(List<DataAccess.Entities.CarChargingEnergyHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => new { x.Timestamp.Year, x.Timestamp.Month })
            .Select(g => new CarChargingEnergyHistoryEntry
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                EnergyCharged = g.Sum(x => x.EnergyCharged)
            })
            .ToList();
    }

    private List<CarChargingEnergyHistoryEntry> GroupEntriesByYear(List<DataAccess.Entities.CarChargingEnergyHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.Timestamp.Year)
            .Select(g => new CarChargingEnergyHistoryEntry
            {
                Date = new DateTime(g.Key, 1, 1),
                EnergyCharged = g.Sum(x => x.EnergyCharged)
            })
            .ToList();
    }
}