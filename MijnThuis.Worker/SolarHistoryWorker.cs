using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Worker;

internal class SolarHistoryWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SolarHistoryWorker> _logger;

    public SolarHistoryWorker(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<SolarHistoryWorker> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // While the service has not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await FetchSolarEnergyHistory(stoppingToken);

                await FetchSolarPowerHistory(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task FetchSolarEnergyHistory(CancellationToken stoppingToken)
    {
        var startHistoryFrom = _configuration.GetValue<DateTime>("SOLAR_HISTORY_START");

        // Calculate the last day of the previous month.
        var previousMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
        var serviceScope = _serviceProvider.CreateScope();
        var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();
        var dbContext = serviceScope.ServiceProvider.GetService<MijnThuisDbContext>();

        await dbContext.Database.MigrateAsync();

        // Gets the latest solar history database entry
        var latestEntry = await dbContext.SolarEnergyHistory.OrderByDescending(x => x.Date).FirstOrDefaultAsync();

        if (latestEntry != null && latestEntry.Date.Year == previousMonth.Year && latestEntry.Date.Month == previousMonth.Month)
        {
            _logger.LogInformation("Solar energy history is up to date.");

            return;
        }

        if (latestEntry != null)
        {
            startHistoryFrom = new DateTime(latestEntry.Date.Year, latestEntry.Date.Month, 1);
        }

        var dateToProcess = startHistoryFrom;
        var now = DateTime.Now;

        while (dateToProcess < previousMonth)
        {
            _logger.LogInformation($"Processing solar energy history for {dateToProcess.Month}/{dateToProcess.Year}...");

            var solarEnergy = await solarService.GetEnergyOverview(dateToProcess);

            var existingEntries = await dbContext.SolarEnergyHistory
                .Where(x => x.Date.Year == dateToProcess.Year && x.Date.Month == dateToProcess.Month)
                .ToListAsync();

            foreach (var measurement in solarEnergy.Chart.Measurements.OrderBy(x => x.MeasurementTime))
            {
                if (!existingEntries.Any(x => x.Date.Date == measurement.MeasurementTime.Date))
                {
                    dbContext.SolarEnergyHistory.Add(new SolarEnergyHistoryEntry
                    {
                        Id = Guid.NewGuid(),
                        Date = measurement.MeasurementTime.Date,
                        DataCollected = now,
                        Import = measurement.Import ?? 0M,
                        Export = measurement.Export ?? 0M,
                        Production = measurement.Production ?? 0M,
                        ProductionToHome = measurement.ProductionDistribution.ToHome ?? 0M,
                        ProductionToBattery = measurement.ProductionDistribution.ToBattery ?? 0M,
                        ProductionToGrid = measurement.ProductionDistribution.ToGrid ?? 0M,
                        Consumption = measurement.Consumption ?? 0M,
                        ConsumptionFromBattery = measurement.ConsumptionDistribution.FromBattery ?? 0M,
                        ConsumptionFromSolar = measurement.ConsumptionDistribution.FromSolar ?? 0M,
                        ConsumptionFromGrid = measurement.ConsumptionDistribution.FromGrid ?? 0M
                    });

                    await dbContext.SaveChangesAsync();
                }
            }

            dateToProcess = dateToProcess.AddMonths(1);
        }
    }

    private async Task FetchSolarPowerHistory(CancellationToken stoppingToken)
    {
        var startHistoryFrom = _configuration.GetValue<DateTime>("SOLAR_HISTORY_START");

        // Calculate yesterdays date.
        var yesterday = DateTime.Today.AddDays(-1);
        var serviceScope = _serviceProvider.CreateScope();
        var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();
        var dbContext = serviceScope.ServiceProvider.GetService<MijnThuisDbContext>();

        await dbContext.Database.MigrateAsync();

        // Gets the latest solar history database entry
        var latestEntry = await dbContext.SolarPowerHistory.OrderByDescending(x => x.Date).FirstOrDefaultAsync();

        if (latestEntry != null && latestEntry.Date == yesterday)
        {
            _logger.LogInformation("Solar power history is up to date.");

            return;
        }

        if (latestEntry != null)
        {
            startHistoryFrom = new DateTime(latestEntry.Date.Year, latestEntry.Date.Month, latestEntry.Date.Day);
        }

        var dateToProcess = startHistoryFrom;
        var now = DateTime.Now;

        while (dateToProcess <= yesterday)
        {
            _logger.LogInformation($"Processing solar power history for {dateToProcess.Day}/{dateToProcess.Month}/{dateToProcess.Year}...");

            var solarPower = await solarService.GetPowerOverview(dateToProcess);

            var existingEntries = await dbContext.SolarPowerHistory
                .Where(x => x.Date.Year == dateToProcess.Year && x.Date.Month == dateToProcess.Month && x.Date.Day == dateToProcess.Day)
                .ToListAsync();

            foreach (var measurement in solarPower.Measurements.OrderBy(x => x.MeasurementTime))
            {
                if (!existingEntries.Any(x => x.Date == measurement.MeasurementTime))
                {
                    dbContext.SolarPowerHistory.Add(new SolarPowerHistoryEntry
                    {
                        Id = Guid.NewGuid(),
                        Date = measurement.MeasurementTime,
                        DataCollected = now,
                        Import = measurement.Import ?? 0M,
                        Export = measurement.Export ?? 0M,
                        Production = measurement.Production ?? 0M,
                        ProductionToHome = measurement.ProductionDistribution.ToHome ?? 0M,
                        ProductionToBattery = measurement.ProductionDistribution.ToBattery ?? 0M,
                        ProductionToGrid = measurement.ProductionDistribution.ToGrid ?? 0M,
                        Consumption = measurement.Consumption ?? 0M,
                        ConsumptionFromBattery = measurement.ConsumptionDistribution.FromBattery ?? 0M,
                        ConsumptionFromSolar = measurement.ConsumptionDistribution.FromSolar ?? 0M,
                        ConsumptionFromGrid = measurement.ConsumptionDistribution.FromGrid ?? 0M,
                        StorageLevel = measurement.StorageLevel ?? 0M
                    });

                    await dbContext.SaveChangesAsync();
                }
            }

            dateToProcess = dateToProcess.AddDays(1);
        }
    }
}