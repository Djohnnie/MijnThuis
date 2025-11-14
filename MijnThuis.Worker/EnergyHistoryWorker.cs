using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Power;
using System;
using System.Diagnostics;
using EnergyHistoryEntry = MijnThuis.DataAccess.Entities.EnergyHistoryEntry;

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
                var flagRepository = serviceScope.ServiceProvider.GetRequiredService<IFlagRepository>();

                var previousEntry = await dbContext.EnergyHistory.OrderByDescending(x => x.Date).FirstOrDefaultAsync();

                if (previousEntry == null || (DateTime.Now - previousEntry.Date).TotalMinutes >= 15)
                {
                    var powerOverview = await powerService.GetOverview();

                    var now = DateTime.Now;
                    var date = new DateTime(now.Year, now.Month, now.Day, now.Hour, (now.Minute / 15) * 15, 0);

                    // Get the energy cost entry for the past hour.
                    var costEntry = await dbContext.DayAheadEnergyPrices
                        .Where(x => x.From == date.AddMinutes(-15))
                        .FirstOrDefaultAsync();

                    var monthlyPowerPeaks = await dbContext.EnergyHistory.GroupBy(x => new { x.Date.Year, x.Date.Month })
                        .Select(x => new
                        {
                            x.Key.Year,
                            x.Key.Month,
                            Date = new DateTime(x.Key.Year, x.Key.Month, 1),
                            PowerPeak = Math.Max(2.5M, x.Skip(1).Select(y => y.MonthlyPowerPeak).Max())
                        }).OrderBy(x => x.Year).ThenBy(x => x.Month).ToListAsync();

                    var electricityTariff = await flagRepository.GetElectricityTariffDetailsFlag();

                    var energyHistoryEntry = new EnergyHistoryEntry
                    {
                        Id = Guid.NewGuid(),
                        Date = date,
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
                    };

                    var variableCost = energyHistoryEntry.TotalImportDelta * (electricityTariff.GreenEnergyContribution + electricityTariff.UsageTariff + electricityTariff.SpecialExciseTax + electricityTariff.EnergyContribution) / 100M;
                    var fixedCost = (electricityTariff.FixedCharge + electricityTariff.DataAdministration) / (365M * 24M * 4M);
                    var averageMonthlyPowerPeak = monthlyPowerPeaks
                        .Where(x => x.Date >= energyHistoryEntry.Date.AddMonths(-12) && x.Date <= energyHistoryEntry.Date.Date)
                        .Average(x => x.PowerPeak);
                    var capacityCost = electricityTariff.CapacityTariff * averageMonthlyPowerPeak / (365M * 24M * 4M);

                    // Calculate the costs based on the 'Day Ahead' energy prices, including VAT in the import cost and recalculated to EUR and not cents.
                    energyHistoryEntry.CalculatedImportCost = energyHistoryEntry.TotalImportDelta * (costEntry != null ? costEntry.ConsumptionCentsPerKWh : 0M) * 1.06M / 100M;
                    energyHistoryEntry.CalculatedExportCost = energyHistoryEntry.TotalExportDelta * -(costEntry != null ? costEntry.InjectionCentsPerKWh : 0M) / 100M;
                    energyHistoryEntry.CalculatedVariableCost = variableCost;
                    energyHistoryEntry.CalculatedFixedCost = fixedCost;
                    energyHistoryEntry.CalculatedCapacityCost = capacityCost;
                    energyHistoryEntry.CalculatedTotalCost = energyHistoryEntry.CalculatedExportCost + energyHistoryEntry.CalculatedImportCost + fixedCost + variableCost + capacityCost;
                    dbContext.EnergyHistory.Add(energyHistoryEntry);

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