using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Power;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Power.Queries;

public class GetEnergyHistoryQueryHandler : IRequestHandler<GetEnergyHistoryQuery, GetEnergyHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetEnergyHistoryQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetEnergyHistoryResponse> Handle(GetEnergyHistoryQuery request, CancellationToken cancellationToken)
    {
        var entries = _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= request.From && x.Date.Date <= request.To);

        var response = new GetEnergyHistoryResponse();

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

    private async Task<List<EnergyHistoryEntry>> GroupEntriesByDay(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Date)
            .Select(g => new EnergyHistoryEntry
            {
                Date = g.Key,
                TotalImport = g.Sum(x => x.TotalImportDelta),
                Tarrif1Import = g.Sum(x => x.Tarrif1ImportDelta),
                Tarrif2Import = g.Sum(x => x.Tarrif2ImportDelta),
                TotalExport = g.Sum(x => x.TotalExportDelta),
                Tarrif1Export = g.Sum(x => x.Tarrif1ExportDelta),
                Tarrif2Export = g.Sum(x => x.Tarrif2ExportDelta),
                TotalGas = g.Sum(x => x.TotalGasDelta),
                TotalGasKwh = g.Sum(x => x.TotalGasKwhDelta),
                MonthlyPowerPeak = g.Max(x => x.MonthlyPowerPeak)
            })
            .ToListAsync();
    }

    private async Task<List<EnergyHistoryEntry>> GroupEntriesByMonth(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new EnergyHistoryEntry
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                TotalImport = g.Sum(x => x.TotalImportDelta),
                Tarrif1Import = g.Sum(x => x.Tarrif1ImportDelta),
                Tarrif2Import = g.Sum(x => x.Tarrif2ImportDelta),
                TotalExport = g.Sum(x => x.TotalExportDelta),
                Tarrif1Export = g.Sum(x => x.Tarrif1ExportDelta),
                Tarrif2Export = g.Sum(x => x.Tarrif2ExportDelta),
                TotalGas = g.Sum(x => x.TotalGasDelta),
                TotalGasKwh = g.Sum(x => x.TotalGasKwhDelta),
                MonthlyPowerPeak = g.Max(x => x.MonthlyPowerPeak)
            })
            .ToListAsync();
    }

    private async Task<List<EnergyHistoryEntry>> GroupEntriesByYear(IQueryable<DataAccess.Entities.EnergyHistoryEntry> entries)
    {
        return await entries
            .GroupBy(x => x.Date.Year)
            .Select(g => new EnergyHistoryEntry
            {
                Date = new DateTime(g.Key, 1, 1),
                TotalImport = g.Sum(x => x.TotalImportDelta),
                Tarrif1Import = g.Sum(x => x.Tarrif1ImportDelta),
                Tarrif2Import = g.Sum(x => x.Tarrif2ImportDelta),
                TotalExport = g.Sum(x => x.TotalExportDelta),
                Tarrif1Export = g.Sum(x => x.Tarrif1ExportDelta),
                Tarrif2Export = g.Sum(x => x.Tarrif2ExportDelta),
                TotalGas = g.Sum(x => x.TotalGasDelta),
                TotalGasKwh = g.Sum(x => x.TotalGasKwhDelta),
                MonthlyPowerPeak = g.Max(x => x.MonthlyPowerPeak)
            })
            .ToListAsync();
    }
}