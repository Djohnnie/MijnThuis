using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess.Entities;

namespace MijnThuis.DataAccess.Repositories;

public interface IDayAheadEnergyPricesRepository
{
    Task<DayAheadEnergyPricesEntry> GetEnergyPriceForTimestamp(DateTime timestamp);
    Task<DayAheadCheapestEnergyPricesEntry> GetCheapestEnergyPriceForTimestamp(DateTime timestamp);
    Task<List<DayAheadEnergyPricesEntry>> GetEnergyPriceForDate(DateTime date, CancellationToken cancellationToken);
    Task<List<DayAheadCheapestEnergyPricesEntry>> GetCheapestEnergyPriceForDate(DateTime date, CancellationToken cancellationToken);
    Task<EnergyPriceRange> GetNegativeInjectionPriceRange();
    Task<List<DayAheadEnergyPricesEntry>> GetLatestEnergyPrices();
    Task AddEnergyPrice(DayAheadEnergyPricesEntry energyPrice);
    Task AddCheapestEnergyPrice(DayAheadCheapestEnergyPricesEntry cheapestEnergyPrice);
    Task<bool> AnyCheapestEnergyPricesOnDate(DateTime date, CancellationToken cancellationToken);
    Task SetCheapestEnergyPriceShouldCharge(Guid id, bool shouldCharge);
}

public class DayAheadEnergyPricesRepository : IDayAheadEnergyPricesRepository
{
    private readonly MijnThuisDbContext _dbContext;

    public DayAheadEnergyPricesRepository(MijnThuisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DayAheadEnergyPricesEntry> GetEnergyPriceForTimestamp(DateTime timestamp)
    {
        return await _dbContext.DayAheadEnergyPrices
            .OrderByDescending(x => x.From)
            .Where(x => x.From <= timestamp && x.To >= timestamp)
            .FirstOrDefaultAsync() ?? new DayAheadEnergyPricesEntry();
    }

    public async Task<DayAheadCheapestEnergyPricesEntry> GetCheapestEnergyPriceForTimestamp(DateTime timestamp)
    {
        return await _dbContext.DayAheadCheapestEnergyPrices
            .OrderBy(x => x.Order)
            .Where(x => x.From <= timestamp && x.To >= timestamp)
            .FirstOrDefaultAsync() ?? new DayAheadCheapestEnergyPricesEntry();
    }

    public async Task<List<DayAheadEnergyPricesEntry>> GetEnergyPriceForDate(DateTime date, CancellationToken cancellationToken)
    {
        var prices = await _dbContext.DayAheadEnergyPrices
            .OrderByDescending(x => x.From)
            .Where(x => x.From.Date == date.Date)
            .ToListAsync(cancellationToken);

        return prices ?? new List<DayAheadEnergyPricesEntry>();
    }

    public async Task<List<DayAheadCheapestEnergyPricesEntry>> GetCheapestEnergyPriceForDate(DateTime date, CancellationToken cancellationToken)
    {
        var prices = await _dbContext.DayAheadCheapestEnergyPrices
            .OrderBy(x => x.Order)
            .Where(x => x.From.Date == date.Date)
            .ToListAsync(cancellationToken);

        return prices ?? new List<DayAheadCheapestEnergyPricesEntry>();
    }

    public async Task<EnergyPriceRange> GetNegativeInjectionPriceRange()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var negativePriceEntries = await _dbContext.DayAheadEnergyPrices
            .Where(x => x.From.Date == today && x.InjectionCentsPerKWh < 0)
            .OrderBy(x => x.From)
            .ToListAsync();

        var description = "Geen negatieve injectietarieven voor vandaag";

        if (!negativePriceEntries.Any() || (negativePriceEntries.LastOrDefault()?.To ?? today.AddDays(1)) < DateTime.Now)
        {
            negativePriceEntries = await _dbContext.DayAheadEnergyPrices
                .Where(x => x.From.Date == tomorrow && x.InjectionCentsPerKWh < 0)
                .OrderBy(x => x.From)
                .ToListAsync();

            if (negativePriceEntries.Any())
            {
                description = $"Negatief injectietarief morgen tussen {negativePriceEntries.First().From.Hour}u en {negativePriceEntries.Last().To.Hour + 1}u";
            }
            else
            {
                description = "Geen negatieve injectietarieven voor morgen";
            }
        }
        else
        {
            description = $"Negatief injectietarief vandaag tussen {negativePriceEntries.First().From.Hour}u en {negativePriceEntries.Last().To.Hour + 1}u";
        }

        return new EnergyPriceRange
        {
            From = negativePriceEntries.FirstOrDefault()?.From ?? null,
            To = negativePriceEntries.LastOrDefault()?.To.AddHours(1) ?? null,
            Description = description
        };
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

    public async Task AddCheapestEnergyPrice(DayAheadCheapestEnergyPricesEntry cheapestEnergyPrice)
    {
        _dbContext.Add(cheapestEnergyPrice);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> AnyCheapestEnergyPricesOnDate(DateTime date, CancellationToken cancellationToken)
    {
        return await _dbContext.DayAheadCheapestEnergyPrices
            .AnyAsync(x => x.From.Date == date.Date, cancellationToken);
    }

    public async Task SetCheapestEnergyPriceShouldCharge(Guid id, bool shouldCharge)
    {
        await _dbContext.DayAheadCheapestEnergyPrices.Where(x => x.Id == id)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.ShouldCharge, shouldCharge));
    }
}

public class EnergyPriceRange
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string Description { get; set; }
}