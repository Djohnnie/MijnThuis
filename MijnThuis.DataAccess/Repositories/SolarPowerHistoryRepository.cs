using Microsoft.EntityFrameworkCore;

namespace MijnThuis.DataAccess.Repositories;

public interface ISolarPowerHistoryRepository
{
    Task<decimal> GetAverageDailyConsumption(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

internal class SolarPowerHistoryRepository : ISolarPowerHistoryRepository
{
    private readonly MijnThuisDbContext _dbContext;

    public SolarPowerHistoryRepository(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetAverageDailyConsumption(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SolarEnergyHistory
            .Where(x => x.Date >= from && x.Date <= to)
            .GroupBy(x => x.Date.Date)
            .Select(x => new
            {
                Date = x.Key,
                Consumption = x.Sum(y => y.Consumption)
            }).AverageAsync(x => x.Consumption, cancellationToken);
    }

    //public async Task<decimal> GetAverageEnergyConsumption(DateTime date, CancellationToken cancellationToken = default)
    //{
    //    var monthStart = new DateTime(date.Year, date.Month, 1);
    //    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

    //    var energyConsumptionPerFifteenMinutes = await _dbContext.SolarEnergyHistory
    //        .Where(x => x.Date >= date && x.Date <= date)
    //        .Select(x => new
    //        {
    //            x.Date,
    //            x.Consumption
    //        })
    //}
}