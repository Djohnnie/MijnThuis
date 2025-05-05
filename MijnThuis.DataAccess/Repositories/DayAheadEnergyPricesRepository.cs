using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess.Repositories;

public interface IDayAheadEnergyPricesRepository
{
    Task AddForDay(DateTime day, decimal[] prices);
}

public class DayAheadEnergyPricesRepository : IDayAheadEnergyPricesRepository
{
    private readonly MijnThuisDbContext _dbContext;

    public DayAheadEnergyPricesRepository(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddForDay(DateTime day, decimal[] prices)
    {
        foreach (var (index, price) in prices.Index())
        {
            _dbContext.Add(new DayAheadEnergyPricesEntry
            {
                Id = Guid.NewGuid(),
                From = day.AddHours(index),
                To = day.AddHours(index + 1).AddSeconds(-1),
                EuroPerMWh = price
            });

            await _dbContext.SaveChangesAsync();
        }
    }
}