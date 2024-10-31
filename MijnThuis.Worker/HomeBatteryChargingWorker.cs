using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Solar;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class HomeBatteryChargingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HomeBatteryChargingWorker> _logger;

    public HomeBatteryChargingWorker(
        IServiceProvider serviceProvider,
        ILogger<HomeBatteryChargingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Solar-forecast:
        // https://doc.forecast.solar/api:estimate
        // https://api.forecast.solar/estimate/:lat/:lon/:dec/:az/:kwp
        // https://api.forecast.solar/estimate/51.06/4.36/39/43/5
        //
        // 6 panels SW: 2.5kWp (223° of 43°, 39°)
        // 3 panelen NE: 1.2kWp (43° of -137°, 38°)
        // 4 panelen SE: 1.6kWp (133° of -47°, 10°)

        const decimal LATITUDE = 51.06M;
        const decimal LONGITUDE = 4.36M;
        const int START_TIME_IN_HOURS = 1;
        const int END_TIME_IN_HOURS = 7;
        const int BATTERY_LEVEL_THRESHOLD = 100;
        const int CHARGING_POWER = 1500;
        const int STANDBY_USAGE = 250;

        DateTime? charged = null;
        DateTime? chargeUntil = null;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Use a timestamp to calculate the duration of the whole process.
                var startTimer = Stopwatch.GetTimestamp();

                using var serviceScope = _serviceProvider.CreateScope();
                var forecastService = serviceScope.ServiceProvider.GetService<IForecastService>();
                var modbusService = serviceScope.ServiceProvider.GetService<IModbusService>();
                var batteryLevel = await modbusService.GetBatteryLevel();

                if (charged.HasValue && chargeUntil.HasValue && DateTime.Now > chargeUntil.Value)
                {
                    _logger.LogInformation($"Battery has been charged until {chargeUntil.Value} and is at level {batteryLevel.Level}!");

                    var retries = 0;
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                        await modbusService.StopChargingBattery();
                        _logger.LogInformation($"Battery has been restored to Maximum Self Consumption storage control mode!");
                    }
                    catch
                    {
                        retries++;

                        if (retries > 5)
                        {
                            throw;
                        }
                    }

                    chargeUntil = null;
                }

                if (charged.HasValue && charged.Value.Date < DateTime.Today.Date)
                {
                    charged = null;
                    _logger.LogInformation("Today is a new day and last charge was yesterday. 'Charged' has been reset!");
                }

                if (charged == null && DateTime.Now.Hour < END_TIME_IN_HOURS && DateTime.Now > DateTime.Today.AddHours(START_TIME_IN_HOURS))
                {
                    _logger.LogInformation($"Today is a new day, and the time is between {START_TIME_IN_HOURS}AM and {END_TIME_IN_HOURS}AM.");

                    var zw6 = await forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, 43M, 2.5M);
                    var no3 = await forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, -137M, 1.2M);
                    var zo4 = await forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 10M, -47M, 1.6M);
                    var totalWattHoursEstimate = zw6.EstimatedWattHoursToday + no3.EstimatedWattHoursToday + zo4.EstimatedWattHoursToday;

                    _logger.LogInformation($"Total estimated solar energy today: {zw6.EstimatedWattHoursToday}Wh (6xZW) + {no3.EstimatedWattHoursToday}Wh (3xNO) + {zo4.EstimatedWattHoursToday}Wh (4xZO) = {totalWattHoursEstimate}Wh");

                    if (batteryLevel.Level < BATTERY_LEVEL_THRESHOLD)
                    {
                        _logger.LogInformation($"Battery level is below {BATTERY_LEVEL_THRESHOLD}%: Prepare for charging at {DateTime.Now}!");

                        var fullEnergy = 9700;
                        var idleEnergy = STANDBY_USAGE * (24 - END_TIME_IN_HOURS);
                        var wattHoursToCharge = fullEnergy - batteryLevel.Level * (fullEnergy / 100M) - totalWattHoursEstimate;

                        if (wattHoursToCharge > 0)
                        {
                            _logger.LogInformation($"Battery level: {batteryLevel.Level}%");
                            _logger.LogInformation($"Idle energy: {idleEnergy}Wh");
                            _logger.LogInformation($"Battery should charge {wattHoursToCharge}Wh!");

                            var maxDuration = (END_TIME_IN_HOURS - START_TIME_IN_HOURS) * 3600;
                            var durationInSeconds = wattHoursToCharge * 3600M / CHARGING_POWER;
                            durationInSeconds = durationInSeconds > maxDuration ? maxDuration : durationInSeconds;
                            var chargingDuration = TimeSpan.FromSeconds((double)durationInSeconds);
                            chargeUntil = DateTime.Now.Add(chargingDuration);

                            var retries = 0;
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                                // Charge the battery with the remaining watt hours.
                                await modbusService.StartChargingBattery(chargingDuration, CHARGING_POWER);
                                _logger.LogInformation($"Battery has been set to Remote Control storage control mode!");
                            }
                            catch
                            {
                                retries++;

                                if (retries > 5)
                                {
                                    throw;
                                }
                            }

                            _logger.LogInformation($"Battery started charging at {CHARGING_POWER}W with a duration of {chargingDuration}.");
                        }
                        else
                        {
                            _logger.LogInformation("Battery should not charge!");
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Battery level is above {BATTERY_LEVEL_THRESHOLD}%: No need to charge at {DateTime.Now}!");
                    }

                    charged = DateTime.Now;
                }


                // Calculate the duration for this whole process.
                var stopTimer = Stopwatch.GetTimestamp();

                // Wait for a maximum of 5 minutes before the next iteration.
                var duration = TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

                if (duration > TimeSpan.Zero)
                {
                    await Task.Delay(duration, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }
}