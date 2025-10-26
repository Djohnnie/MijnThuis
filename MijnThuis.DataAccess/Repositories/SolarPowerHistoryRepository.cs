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
        var startYear = 2024;
        var month = date.Month;

        var energyConsumptionPerFifteenMinutes = await _dbContext.SolarEnergyHistory
            .Where(x => x.Date.Year >= startYear)
            .Where(x => x.Date.Month == month)
            .GroupBy(x => x.Date.TimeOfDay)
            .OrderBy(x => x.Key)
            .Select(x => new ConsumptionPerFifteenMinutes
            {
                TimeOfDay = x.Key,
                Consumption = (int)Math.Round(x.Average(p => p.Consumption))
            }).ToListAsync();

        return energyConsumptionPerFifteenMinutes;
    }
}

public class ConsumptionPerFifteenMinutes
{
    public TimeSpan TimeOfDay { get; set; }
    public int Consumption { get; set; }
}