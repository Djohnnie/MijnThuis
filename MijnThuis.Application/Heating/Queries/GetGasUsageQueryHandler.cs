using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Heating;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Heating.Queries;

public class GetGasUsageQueryHandler : IRequestHandler<GetGasUsageQuery, GetGasUsageResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetGasUsageQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetGasUsageResponse> Handle(GetGasUsageQuery request, CancellationToken cancellationToken)
    {
        var from = request.Unit switch
        {
            GasUsageUnit.Day => new DateTime(request.Date.Year, request.Date.Month, 1),
            GasUsageUnit.Month => new DateTime(request.Date.Year, 1, 1),
            _ => DateTime.MinValue
        };
        var to = request.Unit switch
        {
            GasUsageUnit.Day => new DateTime(request.Date.Year, request.Date.Month, DateTime.DaysInMonth(request.Date.Year, request.Date.Month), 23, 59, 59),
            GasUsageUnit.Month => new DateTime(request.Date.Year, 12, 31, 23, 59, 59),
            _ => DateTime.MaxValue
        };

        var entries = _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= from && x.Date.Date <= to);

        var response = new GetGasUsageResponse();

        switch (request.Unit)
        {
            case GasUsageUnit.Day:
                response.Entries = await GroupEntriesByDay(entries);
                break;
            case GasUsageUnit.Month:
                response.Entries = await GroupEntriesByMonth(entries);
                break;
            case GasUsageUnit.Year:
                response.Entries = await GroupEntriesByYear(entries);
                break;
        }

        // Fill in the missing dates with 0 values
        switch (request.Unit)
        {
            case GasUsageUnit.Day:
                for (var i = 1; i <= DateTime.DaysInMonth(from.Year, from.Month); i++)
                {
                    var day = new DateTime(from.Year, from.Month, i);
                    if (!response.Entries.Any(x => x.Date == day))
                    {
                        response.Entries.Add(new GasUsageEntry
                        {
                            Date = day,
                            GasAmount = 0M
                        });
                    }
                }
                break;
            case GasUsageUnit.Month:
                for (var i = 1; i <= 12; i++)
                {
                    var month = new DateTime(from.Year, i, 1);
                    if (!response.Entries.Any(x => x.Date == month))
                    {
                        response.Entries.Add(new GasUsageEntry
                        {
                            Date = month,
                            GasAmount = 0M
                        });
                    }
                }
                break;
        }

        return response;
    }

    private async Task<List<GasUsageEntry>> GroupEntriesByDay(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Date)
            .Select(g => new GasUsageEntry
            {
                Date = g.Key,
                GasAmount = g.Sum(x => x.TotalGasDelta),
            })
            .ToListAsync();
    }

    private async Task<List<GasUsageEntry>> GroupEntriesByMonth(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new GasUsageEntry
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                GasAmount = g.Sum(x => x.TotalGasDelta),
            })
            .ToListAsync();
    }

    private async Task<List<GasUsageEntry>> GroupEntriesByYear(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Year)
            .Select(g => new GasUsageEntry
            {
                Date = new DateTime(g.Key, 1, 1),
                GasAmount = g.Sum(x => x.TotalGasDelta),
            })
            .ToListAsync();
    }
}