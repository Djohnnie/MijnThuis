using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess.Repositories;

public interface IEnergyForecastsRepository
{
    Task SaveEnergyForecast(EnergyForecastEntry entry);
}

public class EnergyForecastsRepository : IEnergyForecastsRepository
{
    private readonly MijnThuisDbContext _dbContext;

    public EnergyForecastsRepository(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveEnergyForecast(EnergyForecastEntry entry)
    {
        var existingEntry = await _dbContext.EnergyForecasts
            .FirstOrDefaultAsync(e => e.Date == entry.Date);

        if (existingEntry is null)
        {
            _dbContext.EnergyForecasts.Add(entry);
        }
        else
        {
            existingEntry.EnergyConsumptionInWattHours = entry.EnergyConsumptionInWattHours;
            existingEntry.SolarEnergyInWattHours = entry.SolarEnergyInWattHours;
            existingEntry.EstimatedBatteryLevel = entry.EstimatedBatteryLevel;
        }

        await _dbContext.SaveChangesAsync();
    }
}