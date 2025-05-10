using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Power;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Power.Queries;

public class GetEnergyCostHistoryQueryHandler : IRequestHandler<GetEnergyCostHistoryQuery, GetEnergyCostHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetEnergyCostHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetEnergyCostHistoryResponse> Handle(GetEnergyCostHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= request.From && x.Date.Date <= request.To);

        var response = new GetEnergyCostHistoryResponse();

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

        // Fill in the missing dates with 0 values
        switch (request.Unit)
        {
            case EnergyHistoryUnit.Day:
                for (var i = 1; i <= DateTime.DaysInMonth(request.From.Year, request.From.Month); i++)
                {
                    var day = new DateTime(request.From.Year, request.From.Month, i);
                    if (!response.Entries.Any(x => x.Date == day))
                    {
                        response.Entries.Add(new EnergyCostHistoryEntry
                        {
                            Date = day,
                            ImportCost = 0M,
                            ExportCost = 0M
                        });
                    }
                }
                break;
            case EnergyHistoryUnit.Month:
                for (var i = 1; i <= 12; i++)
                {
                    var month = new DateTime(request.From.Year, i, 1);
                    if (!response.Entries.Any(x => x.Date == month))
                    {
                        response.Entries.Add(new EnergyCostHistoryEntry
                        {
                            Date = month,
                            ImportCost = 0M,
                            ExportCost = 0M
                        });
                    }
                }
                break;
        }

        return response;
    }

    private async Task<List<EnergyCostHistoryEntry>> GroupEntriesByDay(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Date)
            .Select(g => new EnergyCostHistoryEntry
            {
                Date = g.Key,
                ImportCost = g.Sum(x => x.CalculatedImportCost),
                ExportCost = g.Sum(x => x.CalculatedExportCost),
                TotalCost = g.Sum(x => x.CalculatedImportCost) + g.Sum(x => x.CalculatedExportCost)
            })
            .ToListAsync();
    }

    private async Task<List<EnergyCostHistoryEntry>> GroupEntriesByMonth(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new EnergyCostHistoryEntry
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                ImportCost = g.Sum(x => x.CalculatedImportCost),
                ExportCost = g.Sum(x => x.CalculatedExportCost),
                TotalCost = g.Sum(x => x.CalculatedImportCost) + g.Sum(x => x.CalculatedExportCost)
            })
            .ToListAsync();
    }

    private async Task<List<EnergyCostHistoryEntry>> GroupEntriesByYear(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Year)
            .Select(g => new EnergyCostHistoryEntry
            {
                Date = new DateTime(g.Key, 1, 1),
                ImportCost = g.Sum(x => x.CalculatedImportCost),
                ExportCost = g.Sum(x => x.CalculatedExportCost),
                TotalCost = g.Sum(x => x.CalculatedImportCost) + g.Sum(x => x.CalculatedExportCost)
            })
            .ToListAsync();
    }
}