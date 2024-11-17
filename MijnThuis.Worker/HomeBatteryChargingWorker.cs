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
        const int START_TIME_IN_HOURS = 1;
        const int END_TIME_IN_HOURS = 6;
        const int BATTERY_LEVEL_THRESHOLD = 100;
        const int CHARGING_POWER = 1500;
        const int STANDBY_USAGE = 250;

        // Keep a flag to know if the battery was charged today.
        DateTime? charged = null;

        // Keep a flag to know when the battery should be charged.
        DateTime? chargeFrom = null;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var forecastService = serviceScope.ServiceProvider.GetService<IForecastService>();
                var modbusService = serviceScope.ServiceProvider.GetService<IModbusService>();

                // Gets the current battery level.
                var batteryLevel = await modbusService.GetBatteryLevel();

                // If the battery is not performing its nightly charge cycle.
                if (!chargeFrom.HasValue)
                {
                    var retries = 0;
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                        // If the battery is not in Maximum Self Consumption storage control mode and not charging from PC and AC
                        if (await modbusService.IsNotMaxSelfConsumption())
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                            // Restore the battery to Maximum Self Consumption storage control mode.
                            await modbusService.StopChargingBattery();

                            _logger.LogInformation($"Battery has been restored to Maximum Self Consumption storage control mode!");
                        }
                    }
                    catch
                    {
                        retries++;

                        if (retries > 5)
                        {
                            throw;
                        }
                    }
                }

                // If the battery has been charged and the charge duration has passed.
                if (charged.HasValue && chargeFrom.HasValue && DateTime.Now > chargeFrom.Value && DateTime.Now > DateTime.Today.AddHours(END_TIME_IN_HOURS))
                {
                    _logger.LogInformation($"Battery has been charged from {chargeFrom.Value} until {DateTime.Today.AddHours(END_TIME_IN_HOURS)} and is at level {batteryLevel.Level}!");

                    var retries = 0;
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                        // Restore the battery to Maximum Self Consumption storage control mode.
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

                    // Reset the chargeFrom flag.
                    chargeFrom = null;
                }

                // If the battery has been charged yesterday, reset the charged flag.
                if (charged.HasValue && charged.Value.Date < DateTime.Today.Date)
                {
                    charged = null;
                    _logger.LogInformation("Today is a new day and last charge was yesterday. 'Charged' has been reset!");
                }

                // If charging the battery has started.
                if (charged.HasValue && chargeFrom.HasValue && DateTime.Now > chargeFrom.Value && DateTime.Now.Hour < END_TIME_IN_HOURS)
                {
                    var retries = 0;
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                        // If the battery is in storage control mode, but not charging from PC and AC
                        if (await modbusService.IsNotChargingInRemoteControlMode())
                        {
                            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                            // Calculate remaining duration to charge the battery.
                            var chargingDuration = DateTime.Today.AddHours(END_TIME_IN_HOURS).AddMinutes(-5) - DateTime.Now;

                            // Restore the battery to Maximum Self Consumption storage control mode.
                            await modbusService.StartChargingBattery(chargingDuration, CHARGING_POWER);

                            _logger.LogInformation($"Battery was in remote control mode, but was not charging! Charging has been restored!");
                        }
                    }
                    catch
                    {
                        retries++;

                        if (retries > 5)
                        {
                            throw;
                        }
                    }
                }

                // If the battery has not been charged today and the chargeFrom flag has a value that is in the past.
                if (charged == null && chargeFrom.HasValue && DateTime.Now > chargeFrom.Value && DateTime.Now.Hour < END_TIME_IN_HOURS)
                {
                    var retries = 0;
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                        // Calculate the charging duration based on the chargeFrom flag and the end time.
                        var chargingDuration = DateTime.Today.AddHours(END_TIME_IN_HOURS).AddMinutes(-5) - chargeFrom.Value;

                        // Charge the battery at the configured charging power for the estimated charge duration.
                        await modbusService.StartChargingBattery(chargingDuration, CHARGING_POWER);
                        _logger.LogInformation($"Battery has been set to Remote Control storage control mode!");
                        _logger.LogInformation($"Battery started charging at {CHARGING_POWER}W with a duration of {chargingDuration} until {DateTime.Today.AddHours(END_TIME_IN_HOURS)}.");
                    }
                    catch
                    {
                        retries++;

                        if (retries > 5)
                        {
                            throw;
                        }
                    }

                    charged = DateTime.Now;
                }

                // If the battery has not been charged today and the time is between START and END.
                if (charged == null && chargeFrom == null && DateTime.Now.Hour < END_TIME_IN_HOURS && DateTime.Now > DateTime.Today.AddHours(START_TIME_IN_HOURS))
                {
                    _logger.LogInformation($"Today is a new day, and the time is between {START_TIME_IN_HOURS}AM and {END_TIME_IN_HOURS}AM.");

                    // Gets the solar forecast estimates for today and for each solar orientation plane.
                    // 6 panels facing ZW, 3 panels facing NO, and 4 panels facing ZO.
                    var forecastOverview = await GetSolarForecast(forecastService);

                    _logger.LogInformation($"Total estimated solar energy today: {forecastOverview.EstimatedWattHoursToday}Wh");
                    _logger.LogInformation($"Sunrise at {forecastOverview.Sunrise} and Sunset at {forecastOverview.Sunset}");

                    // If the battery level is below the threshold.
                    if (batteryLevel.Level < BATTERY_LEVEL_THRESHOLD)
                    {
                        _logger.LogInformation($"Battery level is below {BATTERY_LEVEL_THRESHOLD}%: Prepare for charging at {DateTime.Now}!");

                        // Gets the maximum energy the battery can store.
                        var fullEnergy = batteryLevel.MaxEnergy;
                        // Calculate the estimated idle energy consumption during the night.
                        var idleEnergy = STANDBY_USAGE * (decimal)(forecastOverview.Sunset - forecastOverview.Sunrise).TotalHours;
                        // Calculate the estimated energy to charge, based on the current
                        // charge level, the solar forecast and the estimated ide energy.
                        var wattHoursToCharge = fullEnergy - batteryLevel.Level * (fullEnergy / 100M) - forecastOverview.EstimatedWattHoursToday + idleEnergy;
                        // Calculate the maximum energy that can be charged.
                        var maxWattHoursToCharge = fullEnergy - batteryLevel.Level * (fullEnergy / 100M);
                        // Limit the energy to charge to the maximum energy that can be charged.
                        wattHoursToCharge = wattHoursToCharge > maxWattHoursToCharge ? maxWattHoursToCharge : wattHoursToCharge;

                        // If there is energy to charge.
                        if (wattHoursToCharge > 0)
                        {
                            _logger.LogInformation($"Battery maximum energy: {fullEnergy}Wh");
                            _logger.LogInformation($"Battery level: {batteryLevel.Level}%");
                            _logger.LogInformation($"Idle energy: {(int)idleEnergy}Wh");
                            _logger.LogInformation($"Maximum battery charge: {(int)maxWattHoursToCharge}Wh");
                            _logger.LogInformation($"Battery should charge {(int)wattHoursToCharge}Wh!");

                            // Calculate the maximum duration to charge the battery.
                            var maxDuration = (END_TIME_IN_HOURS - START_TIME_IN_HOURS) * 3600;
                            // Calculate the duration to charge the battery, based on the estimated charge energy.
                            var durationInSeconds = wattHoursToCharge * 3600M / CHARGING_POWER;
                            // Limit the charge duration to the maximum duration.
                            durationInSeconds = durationInSeconds > maxDuration ? maxDuration : durationInSeconds;
                            // Set the chargeFrom flag, by subtracting the charging duration from the end time.
                            var chargingDuration = TimeSpan.FromSeconds((double)durationInSeconds);
                            chargeFrom = DateTime.Today.AddHours(END_TIME_IN_HOURS) - chargingDuration;

                            _logger.LogInformation($"Battery should charge from {chargeFrom}!");
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);

                await Task.Delay(TimeSpan.FromMinutes(5));
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
    }

    private async Task<ForecastOverview> GetSolarForecast(IForecastService forecastService)
    {
        const decimal LATITUDE = 51.06M;
        const decimal LONGITUDE = 4.36M;

        // Gets the solar forecast estimates for today and for each solar orientation plane.
        // 6 panels facing ZW, 3 panels facing NO, and 4 panels facing ZO.
        var zw6 = await forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, 43M, 2.4M);
        var no3 = await forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, -137M, 1.2M);
        var zo4 = await forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 10M, -47M, 1.6M);

        return new ForecastOverview
        {
            EstimatedWattHoursToday = zw6.EstimatedWattHoursToday + no3.EstimatedWattHoursToday + zo4.EstimatedWattHoursToday,
            EstimatedWattHoursTomorrow = zw6.EstimatedWattHoursTomorrow + no3.EstimatedWattHoursTomorrow + zo4.EstimatedWattHoursTomorrow,
            Sunrise = zw6.Sunrise,
            Sunset = zw6.Sunset
        };
    }
}