using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Worker.Helpers;

public interface IHomeBatteryChargingHelper
{
    Task<BatteryCharged> Verify(BatteryCharged battery, CancellationToken cancellationToken);
}

public class HomeBatteryChargingHelper : IHomeBatteryChargingHelper
{
    private readonly IForecastService _forecastService;
    private readonly IModbusService _modbusService;
    private readonly IPowerService _powerService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeBatteryChargingHelper> _logger;

    public HomeBatteryChargingHelper(
        IForecastService forecastService,
        IModbusService modbusService,
        IPowerService powerService,
        IConfiguration configuration,
        ILogger<HomeBatteryChargingHelper> logger)
    {
        _forecastService = forecastService;
        _modbusService = modbusService;
        _powerService = powerService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<BatteryCharged> Verify(BatteryCharged battery, CancellationToken cancellationToken)
    {
        var startTimeInHours = _configuration.GetValue<int>("START_TIME_IN_HOURS");
        var endTimeInHours = _configuration.GetValue<int>("END_TIME_IN_HOURS");
        var gridChargingPower = _configuration.GetValue<int>("GRID_CHARGING_POWER");
        var gridPowerThreshold = _configuration.GetValue<int>("GRID_CHARGING_THRESHOLD");
        var batteryLevelThreshold = _configuration.GetValue<int>("BATTERY_LEVEL_THRESHOLD");
        var standbyUsage = _configuration.GetValue<int>("STANDBY_USAGE");

        // Gets the current battery level.
        var batteryLevel = await _modbusService.GetBatteryLevel();
        var powerOverview = await _powerService.GetOverview();

        // If the battery is not performing its nightly charge cycle.
        if (!battery.ChargeFrom.HasValue)
        {
            var retries = 0;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                // If the battery is not in Maximum Self Consumption storage control mode and not charging from PC and AC
                if (await _modbusService.IsNotMaxSelfConsumption())
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                    // Restore the battery to Maximum Self Consumption storage control mode.
                    await _modbusService.StopChargingBattery();

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
        if (battery.Charged.HasValue && battery.ChargeFrom.HasValue && DateTime.Now > battery.ChargeFrom.Value && DateTime.Now > DateTime.Today.AddHours(endTimeInHours))
        {
            _logger.LogInformation($"Battery has been charged from {battery.ChargeFrom.Value} until {DateTime.Today.AddHours(endTimeInHours)} and is at level {batteryLevel.Level}!");

            var retries = 0;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                // Restore the battery to Maximum Self Consumption storage control mode.
                await _modbusService.StopChargingBattery();
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
            battery.ChargeFrom = null;
        }

        // If the battery has been charged yesterday, reset the charged flag.
        if (battery.Charged.HasValue && battery.Charged.Value.Date < DateTime.Today.Date)
        {
            battery.Charged = null;
            _logger.LogInformation("Today is a new day and last charge was yesterday. 'Charged' has been reset!");
        }

        // If charging the battery has started.
        if (battery.Charged.HasValue && battery.ChargeFrom.HasValue && DateTime.Now > battery.ChargeFrom.Value && DateTime.Now.Hour < endTimeInHours)
        {
            var retries = 0;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                // If the current grid power usage is below the threshold, we can safely continue charging the battery.
                if (powerOverview.CurrentPower < gridPowerThreshold)
                {
                    // If the battery is in storage control mode, but not charging from PC and AC
                    if (await _modbusService.IsNotChargingInRemoteControlMode())
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                        // Calculate remaining duration to charge the battery.
                        var chargingDuration = DateTime.Today.AddHours(endTimeInHours).AddMinutes(-5) - DateTime.Now;

                        // Restore the battery to Maximum Self Consumption storage control mode.
                        await _modbusService.StartChargingBattery(chargingDuration, gridChargingPower);

                        _logger.LogInformation($"Battery was in remote control mode, but was not charging! Charging has been restored!");
                    }
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                    // Restore the battery to Maximum Self Consumption storage control mode.
                    await _modbusService.StopChargingBattery();
                    _logger.LogInformation($"Battery has been temporarily restored to Maximum Self Consumption storage control mode to keep power consumption below the threshold!");

                    battery.Charged = null;
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
        if (battery.Charged == null && battery.ChargeFrom.HasValue && DateTime.Now > battery.ChargeFrom.Value && DateTime.Now.Hour < endTimeInHours)
        {
            var retries = 0;
            try
            {
                // If the current grid power usage is below the threshold, we can safely start charging the battery.
                if (powerOverview.CurrentPower < gridPowerThreshold - gridChargingPower)
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                    // Calculate the charging duration based on the chargeFrom flag and the end time.
                    var chargingDuration = DateTime.Today.AddHours(endTimeInHours).AddMinutes(-5) - battery.ChargeFrom.Value;

                    // Charge the battery at the configured charging power for the estimated charge duration.
                    await _modbusService.StartChargingBattery(chargingDuration, gridChargingPower);
                    _logger.LogInformation($"Battery has been set to Remote Control storage control mode!");
                    _logger.LogInformation($"Battery started charging at {gridChargingPower}W with a duration of {chargingDuration} until {DateTime.Today.AddHours(endTimeInHours)}.");


                    battery.Charged = DateTime.Now;
                }
                else
                {
                    _logger.LogInformation($"Battery should start charging, but the current power consumption of {powerOverview.CurrentPower}W is larger than the threshold!");
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

        // If the battery has not been charged today and the time is between START and END.
        if (battery.Charged == null && battery.ChargeFrom == null && DateTime.Now.Hour < endTimeInHours && DateTime.Now > DateTime.Today.AddHours(startTimeInHours))
        {
            _logger.LogInformation($"Today is a new day, and the time is between {startTimeInHours}AM and {endTimeInHours}AM.");

            // Gets the solar forecast estimates for today and for each solar orientation plane.
            // 6 panels facing ZW, 3 panels facing NO, and 4 panels facing ZO.
            var forecastOverview = await GetSolarForecast();

            _logger.LogInformation($"Total estimated solar energy today: {forecastOverview.EstimatedWattHoursToday}Wh");
            _logger.LogInformation($"Sunrise at {forecastOverview.Sunrise} and Sunset at {forecastOverview.Sunset}");

            // If the battery level is below the threshold.
            if (batteryLevel.Level < batteryLevelThreshold)
            {
                _logger.LogInformation($"Battery level is below {batteryLevelThreshold}%: Prepare for charging at {DateTime.Now}!");

                // Gets the maximum energy the battery can store.
                var fullEnergy = batteryLevel.MaxEnergy;
                // Calculate the estimated idle energy consumption during the night.
                var idleEnergy = standbyUsage * (decimal)(forecastOverview.Sunset - forecastOverview.Sunrise).TotalHours;
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
                    var maxDuration = (endTimeInHours - startTimeInHours) * 3600;
                    // Calculate the duration to charge the battery, based on the estimated charge energy.
                    var durationInSeconds = wattHoursToCharge * 3600M / gridChargingPower;
                    // Limit the charge duration to the maximum duration.
                    durationInSeconds = durationInSeconds > maxDuration ? maxDuration : durationInSeconds;
                    // Set the chargeFrom flag, by subtracting the charging duration from the end time.
                    var chargingDuration = TimeSpan.FromSeconds((double)durationInSeconds);
                    battery.ChargeFrom = DateTime.Today.AddHours(endTimeInHours) - chargingDuration;

                    _logger.LogInformation($"Battery should charge from {battery.ChargeFrom}!");
                }
                else
                {
                    _logger.LogInformation("Battery should not charge!");
                }
            }
            else
            {
                _logger.LogInformation($"Battery level is above {batteryLevelThreshold}%: No need to charge at {DateTime.Now}!");
            }
        }

        return battery;
    }

    private async Task<ForecastOverview> GetSolarForecast()
    {
        const decimal LATITUDE = 51.06M;
        const decimal LONGITUDE = 4.36M;
        const byte DAMPING = 0;

        // Gets the solar forecast estimates for today and for each solar orientation plane.
        // 6 panels facing ZW, 3 panels facing NO, and 4 panels facing ZO.
        var zw6 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, 43M, 2.4M, DAMPING);
        var no3 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, -137M, 1.2M, DAMPING);
        var zo4 = await _forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 10M, -47M, 1.6M, DAMPING);

        return new ForecastOverview
        {
            EstimatedWattHoursToday = zw6.EstimatedWattHoursToday + no3.EstimatedWattHoursToday + zo4.EstimatedWattHoursToday,
            EstimatedWattHoursTomorrow = zw6.EstimatedWattHoursTomorrow + no3.EstimatedWattHoursTomorrow + zo4.EstimatedWattHoursTomorrow,
            Sunrise = zw6.Sunrise,
            Sunset = zw6.Sunset
        };
    }
}

public class BatteryCharged
{
    public BatteryCharged(DateTime? charged, DateTime? chargeFrom)
    {
        Charged = charged;
        ChargeFrom = chargeFrom;
    }

    public DateTime? Charged { get; set; }
    public DateTime? ChargeFrom { get; set; }

    public void Deconstruct(out DateTime? charged, out DateTime? chargeFrom)
    {
        charged = Charged;
        chargeFrom = ChargeFrom;
    }
}