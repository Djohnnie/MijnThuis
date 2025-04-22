using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Query;

internal class GetSolarSelfConsumptionQueryHandler : IRequestHandler<GetSolarSelfConsumptionQuery, GetSolarSelfConsumptionResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public GetSolarSelfConsumptionQueryHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<GetSolarSelfConsumptionResponse> Handle(GetSolarSelfConsumptionQuery request, CancellationToken cancellationToken)
    {
        using var serviceScope = _serviceProvider.CreateScope();
        using var dbContext = serviceScope.ServiceProvider.GetRequiredService<MijnThuisDbContext>();

        var date = request.Date.Date;
        var thisMonth = new DateTime(date.Year, date.Month, 1);
        var thisYear = new DateTime(date.Year, 1, 1);

        var solarEnergyHistoryByDate = await dbContext.SolarEnergyHistory
            .Where(x => x.Date >= thisYear && x.Date.Date <= date)
            .GroupBy(x => x.Date.Date)
            .Select(x => new
            {
                Date = x.Key,
                Import = x.Sum(x => x.Import),
                Export = x.Sum(x => x.ProductionToGrid),
                Production = x.Sum(x => x.Production),
                Consumption = x.Sum(x => x.Consumption)
            })
            .ToListAsync();

        var solarEnergyHistoryToday = solarEnergyHistoryByDate.SingleOrDefault(x => x.Date.Date == date);
        var solarEnergyHistoryThisMonth = solarEnergyHistoryByDate.Where(x => x.Date.Month == thisMonth.Month && x.Date.Year == thisMonth.Year);
        var solarEnergyHistoryThisYear = solarEnergyHistoryByDate.Where(x => x.Date.Year == thisMonth.Year);

        var selfConsumptionToday = solarEnergyHistoryToday != null ? solarEnergyHistoryToday.Production == 0 ? 0M : (solarEnergyHistoryToday.Production - solarEnergyHistoryToday.Export) / solarEnergyHistoryToday.Production * 100M : 0M;
        var selfConsumptionThisMonth = solarEnergyHistoryThisMonth.Any() ? (solarEnergyHistoryThisMonth.Sum(x => x.Production) - solarEnergyHistoryThisMonth.Sum(x => x.Export)) / solarEnergyHistoryThisMonth.Sum(x => x.Production) * 100M : 0M;
        var selfConsumptionThisYear = solarEnergyHistoryThisYear.Any() ? (solarEnergyHistoryThisYear.Sum(x => x.Production) - solarEnergyHistoryThisYear.Sum(x => x.Export)) / solarEnergyHistoryThisYear.Sum(x => x.Production) * 100M : 0M;

        var selfSufficiencyToday = solarEnergyHistoryToday != null ? solarEnergyHistoryToday.Consumption == 0 ? 0M : (solarEnergyHistoryToday.Consumption - solarEnergyHistoryToday.Import) / (solarEnergyHistoryToday.Consumption) * 100M : 0M;
        var selfSufficiencyThisMonth = solarEnergyHistoryThisMonth.Any() ? (solarEnergyHistoryThisMonth.Sum(x => x.Consumption) - solarEnergyHistoryThisMonth.Sum(x => x.Import)) / solarEnergyHistoryThisMonth.Sum(x => x.Consumption) * 100M : 0M;
        var selfSufficiencyThisYear = solarEnergyHistoryThisYear.Any() ? (solarEnergyHistoryThisYear.Sum(x => x.Consumption) - solarEnergyHistoryThisYear.Sum(x => x.Import)) / solarEnergyHistoryThisYear.Sum(x => x.Consumption) * 100M : 0M;

        var from = request.Range switch
        {
            SolarSelfConsumptionRange.Day => new DateTime(date.Year, date.Month, 1),
            SolarSelfConsumptionRange.Month => new DateTime(date.Year, 1, 1),
            SolarSelfConsumptionRange.Year => new DateTime(2000, 1, 1),
            _ => date
        };
        var to = request.Range switch
        {
            SolarSelfConsumptionRange.Day => new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month)),
            SolarSelfConsumptionRange.Month => new DateTime(date.Year, 12, 31),
            SolarSelfConsumptionRange.Year => new DateTime(DateTime.Today.Year, 12, 31),
            _ => date
        };

        var solarEnergyHistoryByRange = await dbContext.SolarEnergyHistory
            .Where(x => x.Date >= from && x.Date.Date <= to)
            .GroupBy(x => x.Date.Date)
            .Select(x => new
            {
                Date = x.Key,
                Import = x.Sum(x => x.Import),
                Export = x.Sum(x => x.ProductionToGrid),
                Production = x.Sum(x => x.Production),
                Consumption = x.Sum(x => x.Consumption)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var entries = new List<SolarSelfConsumptionEntry>();

        switch (request.Range)
        {
            case SolarSelfConsumptionRange.Day:
                entries = solarEnergyHistoryByRange
                    .GroupBy(x => x.Date)
                    .Select(g => new SolarSelfConsumptionEntry
                    {
                        Date = g.Key,
                        SelfConsumption = Math.Max(0M, g.Any() ? Math.Round((g.Sum(x => x.Production) - g.Sum(x => x.Export)) / g.Sum(x => x.Production) * 100M) : 0M),
                        SelfSufficiency = Math.Max(0M, g.Any() ? Math.Round((g.Sum(x => x.Consumption) - g.Sum(x => x.Import)) / g.Sum(x => x.Consumption) * 100M) : 0M)
                    })
                    .ToList();
                break;
            case SolarSelfConsumptionRange.Month:
                entries = solarEnergyHistoryByRange
                    .GroupBy(x => new { x.Date.Year, x.Date.Month })
                    .Select(g => new SolarSelfConsumptionEntry
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        SelfConsumption = Math.Max(0M, g.Any() ? Math.Round((g.Sum(x => x.Production) - g.Sum(x => x.Export)) / g.Sum(x => x.Production) * 100M) : 0M),
                        SelfSufficiency = Math.Max(0M, g.Any() ? Math.Round((g.Sum(x => x.Consumption) - g.Sum(x => x.Import)) / g.Sum(x => x.Consumption) * 100M) : 0M)
                    })
                    .ToList();
                break;
            case SolarSelfConsumptionRange.Year:
                entries = solarEnergyHistoryByRange
                    .GroupBy(x => new { x.Date.Year })
                    .Select(g => new SolarSelfConsumptionEntry
                    {
                        Date = new DateTime(g.Key.Year, 1, 1),
                        SelfConsumption = Math.Max(0M, Math.Round((g.Sum(x => x.Production) == 0 ? 0M : (g.Sum(x => x.Production) - g.Sum(x => x.Export)) / g.Sum(x => x.Production) * 100M))),
                        SelfSufficiency = Math.Max(0M, Math.Round((g.Sum(x => x.Consumption) == 0 ? 0M : (g.Sum(x => x.Consumption) - g.Sum(x => x.Import)) / g.Sum(x => x.Consumption) * 100M)))
                    })
                    .ToList();
                break;
        }

        // Fill in the missing dates with 0 values
        switch (request.Range)
        {
            case SolarSelfConsumptionRange.Day:
                for (var i = 0; i < DateTime.DaysInMonth(date.Year, date.Month); i++)
                {
                    var day = new DateTime(date.Year, date.Month, i + 1);
                    if (!entries.Any(x => x.Date == day))
                    {
                        entries.Add(new SolarSelfConsumptionEntry
                        {
                            Date = day,
                            SelfConsumption = 0M,
                            SelfSufficiency = 0M
                        });
                    }
                }
                break;
            case SolarSelfConsumptionRange.Month:
                for (var i = 1; i <= 12; i++)
                {
                    var month = new DateTime(date.Year, i, 1);
                    if (!entries.Any(x => x.Date == month))
                    {
                        entries.Add(new SolarSelfConsumptionEntry
                        {
                            Date = month,
                            SelfConsumption = 0M,
                            SelfSufficiency = 0M
                        });
                    }
                }
                break;
        }

        var response = new GetSolarSelfConsumptionResponse
        {
            SelfConsumptionToday = Math.Max(0M, Math.Min(100M, selfConsumptionToday)),
            SelfConsumptionThisMonth = Math.Max(0M, Math.Min(100M, selfConsumptionThisMonth)),
            SelfConsumptionThisYear = Math.Max(0M, Math.Min(100M, selfConsumptionThisYear)),
            SelfSufficiencyToday = Math.Max(0M, Math.Min(100M, selfSufficiencyToday)),
            SelfSufficiencyThisMonth = Math.Max(0M, Math.Min(100M, selfSufficiencyThisMonth)),
            SelfSufficiencyThisYear = Math.Max(0M, Math.Min(100M, selfSufficiencyThisYear)),
            Entries = entries
        };

        return response;
    }
}