using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.Integrations.Power;
using System.Diagnostics;

namespace MijnThuis.Worker;

public class EnergyHistoryWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<EnergyHistoryWorker> _logger;

    public EnergyHistoryWorker(
        IConfiguration configuration,
        IServiceScopeFactory serviceProvider,
        ILogger<EnergyHistoryWorker> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int delayInMinutes = 1;

        var gasCoefficient = _configuration.GetValue<decimal>("GAS_COEFFICIENT");

        // While the service has not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var powerService = serviceScope.ServiceProvider.GetService<IPowerService>();
                using var dbContext = serviceScope.ServiceProvider.GetService<MijnThuisDbContext>();

                var previousEntry = await dbContext.EnergyHistory.OrderByDescending(x => x.Date).FirstOrDefaultAsync();

                if (previousEntry == null || (DateTime.Now - previousEntry.Date).TotalHours >= 1)
                {
                    var powerOverview = await powerService.GetOverview();

                    dbContext.EnergyHistory.Add(new EnergyHistoryEntry
                    {
                        Id = Guid.NewGuid(),
                        Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0),
                        ActiveTarrif = powerOverview.ActiveTarrif,
                        TotalImport = powerOverview.TotalImport,
                        TotalImportDelta = previousEntry == null ? powerOverview.TotalImport : powerOverview.TotalImport - previousEntry.TotalImport,
                        Tarrif1Import = powerOverview.Tarrif1Import,
                        Tarrif1ImportDelta = previousEntry == null ? powerOverview.Tarrif1Import : powerOverview.Tarrif1Import - previousEntry.Tarrif1Import,
                        Tarrif2Import = powerOverview.Tarrif2Import,
                        Tarrif2ImportDelta = previousEntry == null ? powerOverview.Tarrif2Import : powerOverview.Tarrif2Import - previousEntry.Tarrif2Import,
                        TotalExport = powerOverview.TotalExport,
                        TotalExportDelta = previousEntry == null ? powerOverview.TotalExport : powerOverview.TotalExport - previousEntry.TotalExport,
                        Tarrif1Export = powerOverview.Tarrif1Export,
                        Tarrif1ExportDelta = previousEntry == null ? powerOverview.Tarrif1Export : powerOverview.Tarrif1Export - previousEntry.Tarrif1Export,
                        Tarrif2Export = powerOverview.Tarrif2Export,
                        Tarrif2ExportDelta = previousEntry == null ? powerOverview.Tarrif2Export : powerOverview.Tarrif2Export - previousEntry.Tarrif2Export,
                        TotalGas = powerOverview.TotalGas,
                        TotalGasDelta = previousEntry == null ? powerOverview.TotalGas : powerOverview.TotalGas - previousEntry.TotalGas,
                        GasCoefficient = gasCoefficient,
                        TotalGasKwh = powerOverview.TotalGas * gasCoefficient,
                        TotalGasKwhDelta = previousEntry == null ? powerOverview.TotalGas * gasCoefficient : (powerOverview.TotalGas - previousEntry.TotalGas) * gasCoefficient,
                        MonthlyPowerPeak = powerOverview.PowerPeak / 1000M
                    });

                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            var stopTimer = Stopwatch.GetTimestamp();

            var duration = TimeSpan.FromMinutes(delayInMinutes) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }
}