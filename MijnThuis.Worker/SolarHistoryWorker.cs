﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.Integrations.Solar;
using System.Diagnostics;

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
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                await FetchSolarEnergyHistory(stoppingToken);

                await FetchSolarPowerHistory(stoppingToken);

                await FetchBatteryEnergyHistory(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            var stopTimer = Stopwatch.GetTimestamp();

            var duration = TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }

    private async Task FetchSolarEnergyHistory(CancellationToken stoppingToken)
    {
        var startHistoryFrom = _configuration.GetValue<DateTime>("SOLAR_HISTORY_START");

        var previousDay = DateTime.Today.AddDays(-1);
        using var serviceScope = _serviceProvider.CreateScope();
        var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();
        using var dbContext = serviceScope.ServiceProvider.GetService<MijnThuisDbContext>();

        // Gets the latest solar history database entry
        var latestEntry = await dbContext.SolarEnergyHistory.OrderByDescending(x => x.Date).FirstOrDefaultAsync();

        if (latestEntry != null && latestEntry.Date == previousDay)
        {
            _logger.LogInformation("Solar energy history is up to date.");

            return;
        }

        if (latestEntry != null)
        {
            startHistoryFrom = latestEntry.Date;
        }

        _logger.LogInformation($"Solar energy history should update from {startHistoryFrom} until {previousDay}.");

        var dateToProcess = startHistoryFrom;
        var now = DateTime.Now;

        while (dateToProcess <= previousDay)
        {
            _logger.LogInformation($"Processing solar energy history for {dateToProcess.Month}/{dateToProcess.Year}...");

            var solarEnergy = await solarService.GetEnergyOverview(dateToProcess);

            var existingEntries = await dbContext.SolarEnergyHistory
                .Where(x => x.Date.Year == dateToProcess.Year && x.Date.Month == dateToProcess.Month)
                .ToListAsync();

            foreach (var measurement in solarEnergy.Chart.Measurements.OrderBy(x => x.MeasurementTime))
            {
                if (measurement.MeasurementTime.Date <= previousDay && !existingEntries.Any(x => x.Date.Date == measurement.MeasurementTime.Date))
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

            dateToProcess = dateToProcess.AddDays(1);
        }

        _logger.LogInformation("Solar energy history has been updated for now.");
    }

    private async Task FetchSolarPowerHistory(CancellationToken stoppingToken)
    {
        var startHistoryFrom = _configuration.GetValue<DateTime>("SOLAR_HISTORY_START");

        var today = DateTime.Today;
        using var serviceScope = _serviceProvider.CreateScope();
        var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();
        using var dbContext = serviceScope.ServiceProvider.GetService<MijnThuisDbContext>();

        // Gets the latest solar history database entry
        var latestEntry = await dbContext.SolarPowerHistory.OrderByDescending(x => x.Date).FirstOrDefaultAsync();

        if (latestEntry != null && (DateTime.Now - latestEntry.Date).TotalMinutes < 5)
        {
            _logger.LogInformation("Solar power history is up to date.");

            return;
        }

        if (latestEntry != null)
        {
            startHistoryFrom = new DateTime(latestEntry.Date.Year, latestEntry.Date.Month, latestEntry.Date.Day);
        }

        _logger.LogInformation($"Solar power history should update from {startHistoryFrom.Day}/{startHistoryFrom.Month}/{startHistoryFrom.Year} until {today.Day}/{today.Month}/{today.Year}.");

        var dateToProcess = startHistoryFrom;
        var now = DateTime.Now;

        while (dateToProcess <= today)
        {
            _logger.LogInformation($"Processing solar power history for {dateToProcess.Day}/{dateToProcess.Month}/{dateToProcess.Year}...");

            var solarPower = await solarService.GetPowerOverview(dateToProcess);

            var existingEntries = await dbContext.SolarPowerHistory
                .Where(x => x.Date.Year == dateToProcess.Year && x.Date.Month == dateToProcess.Month && x.Date.Day == dateToProcess.Day)
                .ToListAsync();

            foreach (var measurement in solarPower.Measurements.OrderBy(x => x.MeasurementTime))
            {
                if (!existingEntries.Any(x => x.Date == measurement.MeasurementTime) && measurement.MeasurementTime < DateTime.Now.AddMinutes(-15))
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

        _logger.LogInformation("Solar power history has been updated for now.");
    }

    private async Task FetchBatteryEnergyHistory(CancellationToken stoppingToken)
    {
        var startHistoryFrom = _configuration.GetValue<DateTime>("SOLAR_HISTORY_START");

        var today = DateTime.Today;
        using var serviceScope = _serviceProvider.CreateScope();
        var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();
        var modbusService = serviceScope.ServiceProvider.GetService<IModbusService>();
        using var dbContext = serviceScope.ServiceProvider.GetService<MijnThuisDbContext>();

        var currentBatteryLevel = await modbusService.GetBatteryLevel();

        // Gets the latest solar history database entry
        var latestEntry = await dbContext.BatteryEnergyHistory.OrderByDescending(x => x.Date).FirstOrDefaultAsync();

        if (latestEntry != null && (DateTime.Now - latestEntry.Date).TotalMinutes < 5)
        {
            _logger.LogInformation("Battery energy history is up to date.");

            return;
        }

        if (latestEntry != null)
        {
            startHistoryFrom = new DateTime(latestEntry.Date.Year, latestEntry.Date.Month, latestEntry.Date.Day);
        }

        _logger.LogInformation($"Battery energy history should update from {startHistoryFrom.Day}/{startHistoryFrom.Month}/{startHistoryFrom.Year} until {today.Day}/{today.Month}/{today.Year}.");

        var dateToProcess = startHistoryFrom;
        var now = DateTime.Now;

        while (dateToProcess <= today)
        {
            _logger.LogInformation($"Processing battery energy history for {dateToProcess.Day}/{dateToProcess.Month}/{dateToProcess.Year}...");

            var batteryEnergy = await solarService.GetBatteryOverview(dateToProcess);

            var existingEntries = await dbContext.BatteryEnergyHistory
                .Where(x => x.Date.Year == dateToProcess.Year && x.Date.Month == dateToProcess.Month && x.Date.Day == dateToProcess.Day)
                .ToListAsync();

            var batteryData = batteryEnergy.Storage.Batteries.SingleOrDefault();

            foreach (var measurement in batteryData.Telemetries.OrderBy(x => x.TimeStamp))
            {
                if (!existingEntries.Any(x => x.Date == measurement.TimeStamp))
                {
                    dbContext.BatteryEnergyHistory.Add(new BatteryEnergyHistoryEntry
                    {
                        Id = Guid.NewGuid(),
                        Date = measurement.TimeStamp,
                        DataCollected = now,
                        RatedEnergy = batteryData.Nameplate,
                        AvailableEnergy = measurement.EnergyAvailable ?? 0M,
                        StateOfCharge = measurement.Level ?? 0M,
                        CalculatedStateOfHealth = (measurement.EnergyAvailable ?? 0M) / batteryData.Nameplate,
                        StateOfHealth = (DateTime.Now - measurement.TimeStamp).TotalHours < 1 ? currentBatteryLevel.Health / 100M : 1M,
                    });

                    await dbContext.SaveChangesAsync();
                }
            }

            dateToProcess = dateToProcess.AddDays(1);
        }

        _logger.LogInformation("Battery energy history has been updated for now.");
    }
}