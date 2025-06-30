using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Power;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Repositories;

namespace MijnThuis.Application.Power.Queries;

public class GetEnergyCostHistoryQueryHandler : IRequestHandler<GetEnergyCostHistoryQuery, GetEnergyCostHistoryResponse>
{
    private readonly MijnThuisDbContext _dbContext;
    private readonly IFlagRepository _flagRepository;

    public GetEnergyCostHistoryQueryHandler(
        MijnThuisDbContext dbContext,
        IFlagRepository flagRepository)
    {
        _dbContext = dbContext;
        _flagRepository = flagRepository;
    }

    public async Task<GetEnergyCostHistoryResponse> Handle(GetEnergyCostHistoryQuery request, CancellationToken cancellationToken)
    {
        var flag = await _flagRepository.GetElectricityTariffDetailsFlag();

        var entries = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= request.From && x.Date.Date <= request.To)
            .GroupBy(x => x.Date.Date)
            .Select(g => new EnergyCostHistoryEntry
            {
                Date = g.Key,
                ImportEnergy = g.Sum(x => x.TotalImportDelta),
                ExportEnergy = g.Sum(x => x.TotalExportDelta),
                EnergyCost = g.Sum(x => x.CalculatedImportCost + x.CalculatedExportCost)
            }).ToListAsync();

        // Skip the first entry in each month because it still has data from the previous month
        var capacityPowers = await _dbContext.EnergyHistory
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(x => new MonthlyPowerPeak
            {
                Date = new DateTime(x.Key.Year, x.Key.Month, 1),
                PowerPeak = Math.Max(2.5M, x.Skip(1).Select(y => y.MonthlyPowerPeak).Max()),
            })
            .ToListAsync();

        foreach (var entry in entries)
        {
            var to = entry.Date.Date;
            var from = to.AddMonths(-6);
            from = new DateTime(from.Year, from.Month, 1);
            var capacityPower = capacityPowers.Where(x => x.Date.Date >= from && x.Date.Date <= to).Average(x => x.PowerPeak);

            entry.CapacityTariffCost = flag.CapacityTariff * capacityPower / 365M + flag.FixedCharge / 365M;
            entry.TransportCost = flag.UsageTariff * entry.ImportEnergy / 100M + flag.DataAdministration / 365M;
            entry.Taxes = flag.GreenEnergyContribution * entry.ImportEnergy / 100M + flag.SpecialExciseTax * entry.ImportEnergy / 100M + flag.EnergyContribution * entry.ImportEnergy / 100M;
        }

        switch (request.Unit)
        {
            case EnergyHistoryUnit.Day:
                entries = GroupEntriesByDay(entries);
                break;
            case EnergyHistoryUnit.Month:
                entries = GroupEntriesByMonth(entries);
                break;
            case EnergyHistoryUnit.Year:
                entries = GroupEntriesByYear(entries);
                break;
        }

        // Fill in the missing dates with 0 values
        switch (request.Unit)
        {
            case EnergyHistoryUnit.Day:
                for (var i = 1; i <= DateTime.DaysInMonth(request.From.Year, request.From.Month); i++)
                {
                    var day = new DateTime(request.From.Year, request.From.Month, i);
                    if (!entries.Any(x => x.Date == day))
                    {
                        entries.Add(new EnergyCostHistoryEntry
                        {
                            Date = day
                        });
                    }
                }
                break;
            case EnergyHistoryUnit.Month:
                for (var i = 1; i <= 12; i++)
                {
                    var month = new DateTime(request.From.Year, i, 1);
                    if (!entries.Any(x => x.Date == month))
                    {
                        entries.Add(new EnergyCostHistoryEntry
                        {
                            Date = month
                        });
                    }
                }
                break;
        }

        return new GetEnergyCostHistoryResponse { Entries = entries };
    }

    private List<EnergyCostHistoryEntry> GroupEntriesByDay(IEnumerable<EnergyCostHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.Date.Date)
            .Select(g => new EnergyCostHistoryEntry
            {
                Date = g.Key,
                ImportEnergy = g.Sum(x => x.ImportEnergy),
                ExportEnergy = g.Sum(x => x.ExportEnergy),
                EnergyCost = g.Sum(x => x.EnergyCost),
                CapacityTariffCost = g.Sum(x => x.CapacityTariffCost),
                TransportCost = g.Sum(x => x.TransportCost),
                Taxes = g.Sum(x => x.Taxes)
            })
            .ToList();
    }

    private List<EnergyCostHistoryEntry> GroupEntriesByMonth(IEnumerable<EnergyCostHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new EnergyCostHistoryEntry
            {
                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                ImportEnergy = g.Sum(x => x.ImportEnergy),
                ExportEnergy = g.Sum(x => x.ExportEnergy),
                EnergyCost = g.Sum(x => x.EnergyCost),
                CapacityTariffCost = g.Sum(x => x.CapacityTariffCost),
                TransportCost = g.Sum(x => x.TransportCost),
                Taxes = g.Sum(x => x.Taxes)
            })
            .ToList();
    }

    private List<EnergyCostHistoryEntry> GroupEntriesByYear(IEnumerable<EnergyCostHistoryEntry> entries)
    {
        return entries
            .GroupBy(x => x.Date.Year)
            .Select(g => new EnergyCostHistoryEntry
            {
                Date = new DateTime(g.Key, 1, 1),
                ImportEnergy = g.Sum(x => x.ImportEnergy),
                ExportEnergy = g.Sum(x => x.ExportEnergy),
                EnergyCost = g.Sum(x => x.EnergyCost),
                CapacityTariffCost = g.Sum(x => x.CapacityTariffCost),
                TransportCost = g.Sum(x => x.TransportCost),
                Taxes = g.Sum(x => x.Taxes)
            })
            .ToList();
    }
}