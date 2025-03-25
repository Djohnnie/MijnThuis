using MediatR;
using Microsoft.EntityFrameworkCore;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess;

namespace MijnThuis.Application.Solar.Query;

internal class GetSolarSelfConsumptionQueryHandler : IRequestHandler<GetSolarSelfConsumptionQuery, GetSolarSelfConsumptionResponse>
{
    private readonly MijnThuisDbContext _dbContext;

    public GetSolarSelfConsumptionQueryHandler(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetSolarSelfConsumptionResponse> Handle(GetSolarSelfConsumptionQuery request, CancellationToken cancellationToken)
    {
        var today = request.Date.Date;
        var thisMonth = new DateTime(today.Year, today.Month, 1);
        var thisYear = new DateTime(today.Year, 1, 1);

        var solarEnergyHistoryByDate = await _dbContext.SolarEnergyHistory
            .Where(x => x.Date >= thisYear && x.Date.Date <= today)
            .GroupBy(x => x.Date.Date)
            .Select(x => new
            {
                Date = x.Key,
                Import = x.Sum(x => x.Import),
                Production = x.Sum(x => x.Production),
                Consumption = x.Sum(x => x.ConsumptionFromBattery) - x.Sum(x => x.ImportToBattery) + x.Sum(x => x.ConsumptionFromSolar)
            })
            .ToListAsync();

        var solarEnergyHistoryToday = solarEnergyHistoryByDate.SingleOrDefault(x => x.Date.Date == today);
        var solarEnergyHistoryThisMonth = solarEnergyHistoryByDate.Where(x => x.Date.Month == thisMonth.Month && x.Date.Year == thisMonth.Year);
        var solarEnergyHistoryThisYear = solarEnergyHistoryByDate.Where(x => x.Date.Year == thisMonth.Year);

        var selfConsumptionToday = solarEnergyHistoryToday != null ? solarEnergyHistoryToday.Production == 0 ? 0M : solarEnergyHistoryToday.Consumption / solarEnergyHistoryToday.Production * 100M : 0M;
        var selfConsumptionThisMonth = solarEnergyHistoryThisMonth.Any() ? solarEnergyHistoryThisMonth.Sum(x => x.Consumption) / solarEnergyHistoryThisMonth.Sum(x => x.Production) * 100M : 0M;
        var selfConsumptionThisYear = solarEnergyHistoryThisYear.Any() ? solarEnergyHistoryThisYear.Sum(x => x.Consumption) / solarEnergyHistoryThisYear.Sum(x => x.Production) * 100M : 0M;

        var selfSufficiencyToday = solarEnergyHistoryToday != null ? solarEnergyHistoryToday.Consumption == 0 ? 0M : (solarEnergyHistoryToday.Consumption - solarEnergyHistoryToday.Import) / solarEnergyHistoryToday.Consumption * 100M : 0M;
        var selfSufficiencyThisMonth = solarEnergyHistoryThisMonth.Any() ? (solarEnergyHistoryThisMonth.Sum(x => x.Consumption) - solarEnergyHistoryThisMonth.Sum(x => x.Import)) / solarEnergyHistoryThisMonth.Sum(x => x.Consumption) * 100M : 0M;
        var selfSufficiencyThisYear = solarEnergyHistoryThisYear.Any() ? (solarEnergyHistoryThisYear.Sum(x => x.Consumption) - solarEnergyHistoryThisYear.Sum(x => x.Import)) / solarEnergyHistoryThisYear.Sum(x => x.Consumption) * 100M : 0M;

        var response = new GetSolarSelfConsumptionResponse
        {
            SelfConsumptionToday = selfConsumptionToday,
            SelfConsumptionThisMonth = selfConsumptionThisMonth,
            SelfConsumptionThisYear = selfConsumptionThisYear,
            SelfSufficiencyToday = selfSufficiencyToday,
            SelfSufficiencyThisMonth = selfSufficiencyThisMonth,
            SelfSufficiencyThisYear = selfSufficiencyThisYear
        };

        return response;
    }
}