using Microsoft.EntityFrameworkCore;

namespace MijnThuis.DataAccess.Repositories;

public interface ISolarPowerHistoryRepository
{
    Task<decimal> GetAverageDailyConsumption(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<List<ConsumptionPerFifteenMinutes>> GetAverageEnergyConsumption(DateTime date, CancellationToken cancellationToken = default);
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

    public async Task<List<ConsumptionPerFifteenMinutes>> GetAverageEnergyConsumption(DateTime date, CancellationToken cancellationToken = default)
    {
        // Anchor the window on the most recent available data so an outage of the
        // history source does not leave the consumption profile empty.
        var latestDate = await _dbContext.SolarEnergyHistory
            .Where(x => x.Date <= date.AddDays(1))
            .MaxAsync(x => (DateTime?)x.Date, cancellationToken);

        if (latestDate is null)
        {
            return new List<ConsumptionPerFifteenMinutes>();
        }

        var aMonthAgo = (latestDate.Value < date ? latestDate.Value : date).AddMonths(-1);

        var energyConsumptionPerFifteenMinutes = await _dbContext.SolarEnergyHistory
            .Where(x => x.Date >= aMonthAgo)
            .GroupBy(x => x.Date.TimeOfDay)
            .OrderBy(x => x.Key)
            .Select(x => new ConsumptionPerFifteenMinutes
            {
                TimeOfDay = x.Key,
                Consumption = (int)Math.Round(x.Average(p => p.Consumption))
            }).ToListAsync(cancellationToken);

        return energyConsumptionPerFifteenMinutes;
    }
}

public class ConsumptionPerFifteenMinutes
{
    public TimeSpan TimeOfDay { get; set; }
    public int Consumption { get; set; }
}