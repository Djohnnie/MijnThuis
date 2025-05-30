﻿using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Solar;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class SolarForecastHistoryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ISolarService _solarService;
    private readonly IForecastService _forecastService;
    private readonly ILogger<SolarHistoryWorker> _logger;

    public SolarForecastHistoryWorker(
        IServiceScopeFactory serviceProvider,
        ISolarService solarService,
        IForecastService forecastService,
        ILogger<SolarHistoryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _solarService = solarService;
        _forecastService = forecastService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime? lastRun = null;

        // While the service has not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                // Gather data just before midnight.
                if ((lastRun == null || lastRun < DateTime.Today) && DateTime.Now.Hour >= 23 && DateTime.Now.Minute >= 45)
                {
                    const decimal LATITUDE = 51.06M;
                    const decimal LONGITUDE = 4.36M;
                    const byte DAMPING = 0;

                    var zw6 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, 43M, 2.4M, DAMPING);
                    var no3 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, -137M, 1.2M, DAMPING);
                    var zo4 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 10M, -47M, 1.6M, DAMPING);
                    var actual = await _solarService.GetEnergy();

                    using var scope = _serviceProvider.CreateScope();
                    using var dbContext = scope.ServiceProvider.GetRequiredService<MijnThuisDbContext>();

                    dbContext.SolarForecastHistory.Add(new SolarForecastHistoryEntry
                    {
                        Id = Guid.CreateVersion7(),
                        Date = DateTime.Today,
                        ForecastedEnergyToday = zw6.EstimatedWattHoursToday,
                        ActualEnergyToday = actual.LastDayEnergy,
                        ForecastedEnergyTomorrow = zw6.EstimatedWattHoursTomorrow,
                        ForecastedEnergyDayAfterTomorrow = zw6.EstimatedWattHoursDayAfterTomorrow,
                        Damping = DAMPING == 1,
                        Declination = 39M,
                        Azimuth = 43M,
                        Power = 2.4M
                    });
                    dbContext.SolarForecastHistory.Add(new SolarForecastHistoryEntry
                    {
                        Id = Guid.CreateVersion7(),
                        Date = DateTime.Today,
                        ForecastedEnergyToday = no3.EstimatedWattHoursToday,
                        ActualEnergyToday = actual.LastDayEnergy,
                        ForecastedEnergyTomorrow = no3.EstimatedWattHoursTomorrow,
                        ForecastedEnergyDayAfterTomorrow = no3.EstimatedWattHoursDayAfterTomorrow,
                        Damping = DAMPING == 1,
                        Declination = 39M,
                        Azimuth = -137M,
                        Power = 1.2M
                    }); dbContext.SolarForecastHistory.Add(new SolarForecastHistoryEntry
                    {
                        Id = Guid.CreateVersion7(),
                        Date = DateTime.Today,
                        ForecastedEnergyToday = zo4.EstimatedWattHoursToday,
                        ActualEnergyToday = actual.LastDayEnergy,
                        ForecastedEnergyTomorrow = zo4.EstimatedWattHoursTomorrow,
                        ForecastedEnergyDayAfterTomorrow = zo4.EstimatedWattHoursDayAfterTomorrow,
                        Damping = DAMPING == 1,
                        Declination = 10M,
                        Azimuth = -47M,
                        Power = 1.6M
                    });

                    await dbContext.SaveChangesAsync();

                    lastRun = DateTime.Now;
                }
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
}