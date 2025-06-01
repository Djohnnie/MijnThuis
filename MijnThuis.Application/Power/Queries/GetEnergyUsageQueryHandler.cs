using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Power.Queries;

public class GetEnergyUsageQueryHandler : IRequestHandler<GetEnergyUsageQuery, GetEnergyUsageResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetEnergyUsageQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetEnergyUsageResponse> Handle(GetEnergyUsageQuery request, CancellationToken cancellationToken)
    {
        var from = request.Unit switch
        {
            PowerUsageUnit.Day => new DateTime(request.Date.Year, request.Date.Month, 1),
            PowerUsageUnit.Month => new DateTime(request.Date.Year, 1, 1),
            _ => DateTime.MinValue
        };
        var to = request.Unit switch
        {
            PowerUsageUnit.Day => new DateTime(request.Date.Year, request.Date.Month, DateTime.DaysInMonth(request.Date.Year, request.Date.Month), 23, 59, 59),
            PowerUsageUnit.Month => new DateTime(request.Date.Year, 12, 31, 23, 59, 59),
            _ => DateTime.MaxValue
        };

        var entries = _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= from && x.Date.Date <= to);

        var response = new GetEnergyUsageResponse();

        switch (request.Unit)
        {
            case PowerUsageUnit.Day:
                response.Entries = await GroupEntriesByDay(entries);
                break;
            case PowerUsageUnit.Month:
                response.Entries = await GroupEntriesByMonth(entries);
                break;
            case PowerUsageUnit.Year:
                response.Entries = await GroupEntriesByYear(entries);
                break;
        }

        // Fill in the missing dates with 0 values
        switch (request.Unit)
        {
            case PowerUsageUnit.Day:
                for (var i = 1; i <= DateTime.DaysInMonth(from.Year, from.Month); i++)
                {
                    var day = new DateTime(from.Year, from.Month, i);
                    if (!response.Entries.Any(x => x.Date == day))
                    {
                        response.Entries.Add(new PowerUsageEntry
                        {
                            Date = day,
                            EnergyImport = 0M,
                            EnergyExport = 0M
                        });
                    }
                }
                break;
            case PowerUsageUnit.Month:
                for (var i = 1; i <= 12; i++)
                {
                    var month = new DateTime(from.Year, i, 1);
                    if (!response.Entries.Any(x => x.Date == month))
                    {
                        response.Entries.Add(new PowerUsageEntry
                        {
                            Date = month,
                            EnergyImport = 0M,
                            EnergyExport = 0M
                        });
                    }
                }
                break;
        }

        return response;
    }

    private async Task<List<PowerUsageEntry>> GroupEntriesByDay(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Date)
            .Select(g => new PowerUsageEntry
            {
                Date = g.Key,
                EnergyImport = g.Sum(x => x.TotalImportDelta),
                EnergyExport = g.Sum(x => x.TotalExportDelta)
            })
            .ToListAsync();
    }

    private async Task<List<PowerUsageEntry>> GroupEntriesByMonth(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new PowerUsageEntry
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                EnergyImport = g.Sum(x => x.TotalImportDelta),
                EnergyExport = g.Sum(x => x.TotalExportDelta)
            })
            .ToListAsync();
    }

    private async Task<List<PowerUsageEntry>> GroupEntriesByYear(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Year)
            .Select(g => new PowerUsageEntry
            {
                Date = new DateTime(g.Key, 1, 1),
                EnergyImport = g.Sum(x => x.TotalImportDelta),
                EnergyExport = g.Sum(x => x.TotalExportDelta)
            })
            .ToListAsync();
    }
}