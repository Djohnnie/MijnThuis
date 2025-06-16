using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess;
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
                const decimal LATITUDE = 51.06M;
                const decimal LONGITUDE = 4.36M;
                const byte DAMPING = 0;

                using var scope = _serviceProvider.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<MijnThuisDbContext>();

                var mostRecentEntryToday = await dbContext.SolarForecastPeriods
                    .OrderByDescending(x => x.DataFetched)
                    .FirstOrDefaultAsync(x => x.Timestamp.Date == DateTime.Today);

                if (mostRecentEntryToday is null || (DateTime.Now - mostRecentEntryToday.DataFetched).TotalMinutes > 30)
                {
                    var now = DateTime.Now;
                    var today = DateTime.Today;

                    var zw6 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 28M, 43M, 2.4M, DAMPING);
                    var no3 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 33M, -137M, 1.2M, DAMPING);
                    var zo4 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 12M, -47M, 1.6M, DAMPING);
                    var actual = await _solarService.GetEnergyOverview(today);

                    var zw6Today = zw6.WattHourPeriods.Where(x => x.Timestamp.Date == today).ToList();
                    var zw6Today1 = zw6.WattHourPeriods.Where(x => x.Timestamp.Date == today.AddDays(1)).ToList();
                    var zw6Today2 = zw6.WattHourPeriods.Where(x => x.Timestamp.Date == today.AddDays(2)).ToList();
                    var zw6Today3 = zw6.WattHourPeriods.Where(x => x.Timestamp.Date == today.AddDays(3)).ToList();
                    var no3Today = no3.WattHourPeriods.Where(x => x.Timestamp.Date == today).ToList();
                    var no3Today1 = no3.WattHourPeriods.Where(x => x.Timestamp.Date == today.AddDays(1)).ToList();
                    var no3Today2 = no3.WattHourPeriods.Where(x => x.Timestamp.Date == today.AddDays(2)).ToList();
                    var no3Today3 = no3.WattHourPeriods.Where(x => x.Timestamp.Date == today.AddDays(3)).ToList();
                    var zo4Today = zo4.WattHourPeriods.Where(x => x.Timestamp.Date == today).ToList();
                    var zo4Today1 = zo4.WattHourPeriods.Where(x => x.Timestamp.Date == today.AddDays(1)).ToList();
                    var zo4Today2 = zo4.WattHourPeriods.Where(x => x.Timestamp.Date == today.AddDays(2)).ToList();
                    var zo4Today3 = zo4.WattHourPeriods.Where(x => x.Timestamp.Date == today.AddDays(3)).ToList();

                    await ProcessPeriods(dbContext, now, today, zw6Today, no3Today, zo4Today, actual);
                    await ProcessPeriods(dbContext, now, today.AddDays(1), zw6Today1, no3Today1, zo4Today1, actual);
                    await ProcessPeriods(dbContext, now, today.AddDays(2), zw6Today2, no3Today2, zo4Today2, actual);
                    await ProcessPeriods(dbContext, now, today.AddDays(3), zw6Today3, no3Today3, zo4Today3, actual);

                    await dbContext.SaveChangesAsync();
                }

                // Gather historic data just before midnight.
                if ((lastRun == null || lastRun < DateTime.Today) && DateTime.Now.Hour >= 23 && DateTime.Now.Minute >= 45)
                {
                    var zw6 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 28M, 43M, 2.4M, DAMPING);
                    var no3 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 33M, -137M, 1.2M, DAMPING);
                    var zo4 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 12M, -47M, 1.6M, DAMPING);
                    var actual = await _solarService.GetEnergy();

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

    private static async Task ProcessPeriods(MijnThuisDbContext dbContext, DateTime now, DateTime date, List<WattHourPeriod> zw6, List<WattHourPeriod> no3, List<WattHourPeriod> zo4, EnergyOverviewResponse actual)
    {
        for (var i = 0; i < zw6.Count; i++)
        {
            var periodZw6 = zw6[i];
            var periodNo3 = no3[i];
            var periodZo4 = zo4[i];

            if (i == 0)
            {
                periodZw6.Timestamp = zw6[i + 1].Timestamp.AddMinutes(-30);
                periodNo3.Timestamp = no3[i + 1].Timestamp.AddMinutes(-30);
                periodZo4.Timestamp = zo4[i + 1].Timestamp.AddMinutes(-30);
            }

            if (i == zw6.Count - 1)
            {
                periodZw6.Timestamp = zw6[i - 1].Timestamp.AddMinutes(30);
                periodNo3.Timestamp = no3[i - 1].Timestamp.AddMinutes(30);
                periodZo4.Timestamp = zo4[i - 1].Timestamp.AddMinutes(30);
            }
        }

        for (var i = 0; i < zw6.Count; i++)
        {
            var period = zw6[i];
            if (period.Timestamp.Date == date)
            {
                var existingPeriod = await dbContext.SolarForecastPeriods
                    .FirstOrDefaultAsync(x => x.Timestamp == period.Timestamp);

                var actualMeasurement1 = actual.Chart.Measurements.FirstOrDefault(x => x.MeasurementTime == period.Timestamp);
                var actualMeasurement2 = actual.Chart.Measurements.FirstOrDefault(x => x.MeasurementTime == period.Timestamp.AddMinutes(15));

                var forecastedEnergy = zw6[i].WattHours + no3[i].WattHours + zo4[i].WattHours;
                var actualEnergy = (actualMeasurement1?.Production ?? 0) + (actualMeasurement2?.Production ?? 0);

                if (existingPeriod != null)
                {
                    if (existingPeriod.ForecastedEnergy != forecastedEnergy)
                    {
                        existingPeriod.ForecastedEnergy = forecastedEnergy;
                    }

                    if (existingPeriod.ActualEnergy != actualEnergy)
                    {
                        existingPeriod.ActualEnergy = actualEnergy;
                    }

                    existingPeriod.DataFetched = now;

                    continue;
                }

                dbContext.SolarForecastPeriods.Add(new SolarForecastPeriodEntry
                {
                    Id = Guid.CreateVersion7(),
                    Timestamp = period.Timestamp,
                    DataFetched = now,
                    ForecastedEnergy = forecastedEnergy,
                    ActualEnergy = actualEnergy,
                });
            }
        }
    }
}