using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess.Repositories;

public interface IDayAheadEnergyPricesRepository
{
    Task<List<DayAheadEnergyPricesEntry>> GetLatestEnergyPrices();
    Task AddEnergyPrice(DayAheadEnergyPricesEntry energyPrice);
}

public class DayAheadEnergyPricesRepository : IDayAheadEnergyPricesRepository
{
    private readonly MijnThuisDbContext _dbContext;

    public DayAheadEnergyPricesRepository(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<DayAheadEnergyPricesEntry>> GetLatestEnergyPrices()
    {
        var latestEntry = await _dbContext.DayAheadEnergyPrices
            .OrderByDescending(x => x.From)
            .FirstOrDefaultAsync();

        if (latestEntry is null)
        {
            return new();
        }

        var latestPrices = await _dbContext.DayAheadEnergyPrices
            .Where(x => x.From.Date == latestEntry.From.Date)
            .OrderBy(x => x.From)
            .ToListAsync();

        return latestPrices;
    }

    public async Task AddEnergyPrice(DayAheadEnergyPricesEntry energyPrice)
    {
        _dbContext.Add(energyPrice);
        await _dbContext.SaveChangesAsync();
    }
}