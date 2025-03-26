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

        var today = request.Date.Date;
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        var thisYear = new DateTime(today.Year, 1, 1);

        var solarEnergyHistoryByDate = await dbContext.SolarEnergyHistory
            .Where(x => x.Date >= thisYear && x.Date.Date <= today)
            .GroupBy(x => x.Date.Date)
            .Select(x => new
            {
                Date = x.Key,
                Import = x.Sum(x => x.Import),
                Export = x.Sum(x => x.ProductionToGrid),
                Production = x.Sum(x => x.Production),
                Consumption = x.Sum(x => x.ConsumptionFromBattery) - x.Sum(x => x.ImportToBattery) + x.Sum(x => x.ConsumptionFromSolar)
            })
            .ToListAsync();

        var solarEnergyHistoryToday = solarEnergyHistoryByDate.SingleOrDefault(x => x.Date.Date == today);
        var solarEnergyHistoryThisMonth = solarEnergyHistoryByDate.Where(x => x.Date.Month == thisMonth.Month && x.Date.Year == thisMonth.Year);
        var solarEnergyHistoryThisYear = solarEnergyHistoryByDate.Where(x => x.Date.Year == thisMonth.Year);

        var selfConsumptionToday = solarEnergyHistoryToday != null ? solarEnergyHistoryToday.Production == 0 ? 0M : (solarEnergyHistoryToday.Production - solarEnergyHistoryToday.Export) / solarEnergyHistoryToday.Production * 100M : 0M;
        var selfConsumptionThisMonth = solarEnergyHistoryThisMonth.Any() ? (solarEnergyHistoryThisMonth.Sum(x => x.Production) - solarEnergyHistoryThisMonth.Sum(x => x.Export)) / solarEnergyHistoryThisMonth.Sum(x => x.Production) * 100M : 0M;
        var selfConsumptionThisYear = solarEnergyHistoryThisYear.Any() ? (solarEnergyHistoryThisYear.Sum(x => x.Production) - solarEnergyHistoryThisMonth.Sum(x => x.Export)) / solarEnergyHistoryThisYear.Sum(x => x.Production) * 100M : 0M;

        var selfSufficiencyToday = solarEnergyHistoryToday != null ? solarEnergyHistoryToday.Consumption == 0 ? 0M : (solarEnergyHistoryToday.Consumption - solarEnergyHistoryToday.Import) / solarEnergyHistoryToday.Consumption * 100M : 0M;
        var selfSufficiencyThisMonth = solarEnergyHistoryThisMonth.Any() ? (solarEnergyHistoryThisMonth.Sum(x => x.Consumption) - solarEnergyHistoryThisMonth.Sum(x => x.Import)) / solarEnergyHistoryThisMonth.Sum(x => x.Consumption) * 100M : 0M;
        var selfSufficiencyThisYear = solarEnergyHistoryThisYear.Any() ? (solarEnergyHistoryThisYear.Sum(x => x.Consumption) - solarEnergyHistoryThisYear.Sum(x => x.Import)) / solarEnergyHistoryThisYear.Sum(x => x.Consumption) * 100M : 0M;

        var response = new GetSolarSelfConsumptionResponse
        {
            SelfConsumptionToday = Math.Min(100M, selfConsumptionToday),
            SelfConsumptionThisMonth = Math.Min(100M, selfConsumptionThisMonth),
            SelfConsumptionThisYear = Math.Min(100M, selfConsumptionThisYear),
            SelfSufficiencyToday = Math.Min(100M, selfSufficiencyToday),
            SelfSufficiencyThisMonth = Math.Min(100M, selfSufficiencyThisMonth),
            SelfSufficiencyThisYear = Math.Min(100M, selfSufficiencyThisYear)
        };

        return response;
    }
}