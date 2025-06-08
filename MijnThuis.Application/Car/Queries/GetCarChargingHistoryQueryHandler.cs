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
        var entries = await _dbContext.CarChargesHistory
            .Where(x => x.StartedAt.Date >= request.From && x.StartedAt.Date <= request.To)
            .Where(x => x.LocationFriendlyName == "Thuis")
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

    private List<CarChargingEnergyHistoryEntry> GroupEntriesByDay(List<DataAccess.Entities.CarChargesHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.StartedAt.Date)
            .Select(g => new CarChargingEnergyHistoryEntry
            {
                Date = g.Key,
                EnergyCharged = g.Sum(x => x.EnergyAdded),
                EnergyUsed = g.Sum(x => x.EnergyUsed)
            })
            .ToList();
    }

    private List<CarChargingEnergyHistoryEntry> GroupEntriesByMonth(List<DataAccess.Entities.CarChargesHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => new { x.StartedAt.Year, x.StartedAt.Month })
            .Select(g => new CarChargingEnergyHistoryEntry
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                EnergyCharged = g.Sum(x => x.EnergyAdded),
                EnergyUsed = g.Sum(x => x.EnergyUsed)
            })
            .ToList();
    }

    private List<CarChargingEnergyHistoryEntry> GroupEntriesByYear(List<DataAccess.Entities.CarChargesHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.StartedAt.Year)
            .Select(g => new CarChargingEnergyHistoryEntry
            {
                Date = new DateTime(g.Key, 1, 1),
                EnergyCharged = g.Sum(x => x.EnergyAdded),
                EnergyUsed = g.Sum(x => x.EnergyUsed)
            })
            .ToList();
    }
}