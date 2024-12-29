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
        // Calculate the last day of the previous month.
        var previousMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);

        // While the service has not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var startHistoryFrom = _configuration.GetValue<DateTime>("SOLAR_HISTORY_START");

                using var serviceScope = _serviceProvider.CreateScope();
                var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();
                var dbContext = serviceScope.ServiceProvider.GetService<MijnThuisDbContext>();

                await dbContext.Database.MigrateAsync();

                // Gets the latest solar history database entry
                var latestEntry = await dbContext.SolarHistory.OrderByDescending(x => x.Date).FirstOrDefaultAsync();

                if (latestEntry != null && latestEntry.Date.Year == previousMonth.Year && latestEntry.Date.Month == previousMonth.Month)
                {
                    _logger.LogInformation("Solar history is up to date.");

                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                    continue;
                }

                if (latestEntry != null)
                {
                    startHistoryFrom = new DateTime(latestEntry.Date.Year, latestEntry.Date.Month, 1);
                }

                var dateToProcess = startHistoryFrom;
                var now = DateTime.Now;

                while (dateToProcess < previousMonth)
                {
                    _logger.LogInformation($"Processing solar history for {dateToProcess.Month}/{dateToProcess.Year}...");

                    var solarEnergy = await solarService.GetEnergyOverview(dateToProcess);

                    var existingEntries = await dbContext.SolarHistory
                        .Where(x => x.Date.Year == dateToProcess.Year && x.Date.Month == dateToProcess.Month)
                        .ToListAsync();

                    foreach (var measurement in solarEnergy.Chart.Measurements.OrderBy(x => x.MeasurementTime))
                    {
                        if (!existingEntries.Any(x => x.Date.Date == measurement.MeasurementTime.Date))
                        {
                            dbContext.SolarHistory.Add(new SolarHistoryEntry
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
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}