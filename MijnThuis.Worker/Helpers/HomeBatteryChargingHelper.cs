using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Worker.Helpers;

public interface IHomeBatteryChargingHelper
{
    Task UpdateChargingSchedule(CancellationToken cancellationToken);
    Task CheckForBatteryCharging(CancellationToken cancellationToken);
    Task PrepareCheapestPeriods(CancellationToken cancellationToken);
}

public class HomeBatteryChargingHelper : IHomeBatteryChargingHelper
{
    private readonly IForecastService _forecastService;
    private readonly IModbusService _modbusService;
    private readonly IConfiguration _configuration;
    private readonly IDayAheadEnergyPricesRepository _dayAheadEnergyPricesRepository;
    private readonly ISolarPowerHistoryRepository _solarPowerHistoryRepository;
    private readonly IEnergyForecastsRepository _energyForecastsRepository;
    private readonly ILogger<HomeBatteryChargingHelper> _logger;

    public HomeBatteryChargingHelper(
        IForecastService forecastService,
        IModbusService modbusService,
        IConfiguration configuration,
        IDayAheadEnergyPricesRepository dayAheadEnergyPricesRepository,
        ISolarPowerHistoryRepository solarPowerHistoryRepository,
        IEnergyForecastsRepository energyForecastsRepository,
        ILogger<HomeBatteryChargingHelper> logger)
    {
        _forecastService = forecastService;
        _modbusService = modbusService;
        _configuration = configuration;
        _dayAheadEnergyPricesRepository = dayAheadEnergyPricesRepository;
        _solarPowerHistoryRepository = solarPowerHistoryRepository;
        _energyForecastsRepository = energyForecastsRepository;
        _logger = logger;
    }

    public async Task UpdateChargingSchedule(CancellationToken cancellationToken)
    {
        var gridChargingPower = _configuration.GetValue<int>("GRID_CHARGING_POWER");

        // Gets the solar forecast.
        var solarForecast = await GetSolarForecast();

        // Gets current battery level to calculate how much energy we need to charge.
        var batteryLevel = await _modbusService.GetBatteryLevel();
        var currentBatteryLevel = batteryLevel.Level;
        var maximumBatteryEnergy = batteryLevel.MaxEnergy;
        var remainingBatteryEnergy = (int)(currentBatteryLevel / 100M * maximumBatteryEnergy);
        var now = DateTime.Now.TimeOfDay;

        // Estimate when battery will be empty based on historic consumption.
        var averageEnergyConsumption = await _solarPowerHistoryRepository.GetAverageEnergyConsumption(DateTime.Today);
        ReorderAverageEnergyConsumtion(averageEnergyConsumption, now);
        var cumulativeConsumption = 0;
        var cumulativeBatteryLevel = (int)currentBatteryLevel;
        var estimatedEmptyBatteryTime = DateTime.MinValue;
        var currentDateTime = DateTime.Today;
        for (int i = 0; i < averageEnergyConsumption.Count; i++)
        {
            var timeOfDay = averageEnergyConsumption[i].TimeOfDay;

            var dateTime = timeOfDay >= now ? DateTime.Today.Add(timeOfDay) : DateTime.Today.AddDays(1).Add(timeOfDay);
            currentDateTime = dateTime > currentDateTime ? dateTime : DateTime.Today.AddDays(1).Add(timeOfDay);

            var forecastedSolarEnergy = FindForecastedSolarEnergy(solarForecast, currentDateTime);

            // If manually charging is enabled
            var cheapestEnergyPriceEntry = await _dayAheadEnergyPricesRepository.GetCheapestEnergyPriceForTimestamp(currentDateTime);
            if (cheapestEnergyPriceEntry is not null && cheapestEnergyPriceEntry.ShouldCharge)
            {
                var energyFromGrid = (int)Math.Round(gridChargingPower / 4M); // 15 minutes of charging at grid power
                if (forecastedSolarEnergy < energyFromGrid)
                {
                    forecastedSolarEnergy = energyFromGrid;
                }
            }

            var consumption = averageEnergyConsumption[i].Consumption - forecastedSolarEnergy;
            cumulativeConsumption += consumption;
            cumulativeBatteryLevel -= (int)Math.Round(consumption / maximumBatteryEnergy * 100M);
            cumulativeBatteryLevel = Math.Clamp(cumulativeBatteryLevel, 0, 100);

            await _energyForecastsRepository.SaveEnergyForecast(new EnergyForecastEntry
            {
                Date = currentDateTime,
                EnergyConsumptionInWattHours = averageEnergyConsumption[i].Consumption,
                SolarEnergyInWattHours = forecastedSolarEnergy,
                EstimatedBatteryLevel = cumulativeBatteryLevel
            });

            //if (cumulativeConsumption >= remainingBatteryEnergy)
            //{
            //    estimatedEmptyBatteryTime = currentDateTime;
            //    break;
            //}
        }

        //// Start scheduling the next day from 6 PM today.
        //var dayToSchedule = DateTime.Now.Hour < 18 ? DateTime.Today : DateTime.Today.AddDays(1);
        //var calculatedSolarForecastOnDayToSchedule = solarForecast.WattHourPeriods
        //    .Where(x => x.Timestamp.Date == dayToSchedule)
        //    .Sum(x => x.WattHours);
        //var cheapestPricesToday = await _dayAheadEnergyPricesRepository.GetCheapestEnergyPriceForDate(dayToSchedule, cancellationToken);
        //var averageDailyEnergy = await _solarPowerHistoryRepository.GetAverageDailyConsumption(dayToSchedule.AddDays(-7), dayToSchedule, cancellationToken);
        //var batteryEnergyToCharge = 97 * (90 - batteryLevel.Level);
        //var totalEnergyNeeded = (int)averageDailyEnergy + (int)batteryEnergyToCharge - calculatedSolarForecastOnDayToSchedule;

        //var numberOf15MinuteBlocksNeeded = (int)(totalEnergyNeeded / (gridChargingPower / 4M));
        //var numberOf15MinuteBlocksMarked = 0;
        //for (var i = 0; i < cheapestPricesToday.Count; i++)
        //{
        //    if (cheapestPricesToday[i].From > DateTime.Now)
        //    {
        //        await _dayAheadEnergyPricesRepository.SetCheapestEnergyPriceShouldCharge(cheapestPricesToday[i].Id, numberOf15MinuteBlocksMarked < numberOf15MinuteBlocksNeeded);
        //        numberOf15MinuteBlocksMarked++;
        //    }
        //}
    }

    private int FindForecastedSolarEnergy(ForecastOverview solarForecast, DateTime dateTime)
    {
        // Normalize to the nearest 30-minute interval.
        dateTime = dateTime.Minute == 0 || dateTime.Minute == 15
            ? new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0)
            : new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 30, 0);

        var period = solarForecast.WattHourPeriods.FirstOrDefault(x => x.Timestamp == dateTime);

        // Each period represents 30 minutes, so we take half of the watt hours for 15 minutes.
        return period != null ? (int)Math.Round(period.WattHours / 2M) : 0;
    }

    private void ReorderAverageEnergyConsumtion(List<ConsumptionPerFifteenMinutes> averageEnergyConsumption, TimeSpan now)
    {
        // Find the index where we should split the list (first entry at or after the current time)
        var splitIndex = averageEnergyConsumption.FindIndex(x => x.TimeOfDay >= now);

        // If no entry is found at or after the current time, no reordering is needed
        if (splitIndex == -1)
            return;

        // Split the list: entries before current time
        var entriesBeforeNow = averageEnergyConsumption.Take(splitIndex).ToList();

        // Split the list: entries at or after current time
        var entriesFromNow = averageEnergyConsumption.Skip(splitIndex).ToList();

        // Clear the original list and rebuild it with entries from now first, then entries before now
        averageEnergyConsumption.Clear();
        averageEnergyConsumption.AddRange(entriesFromNow);
        averageEnergyConsumption.AddRange(entriesBeforeNow);
        averageEnergyConsumption.AddRange(entriesFromNow);
    }

    public async Task CheckForBatteryCharging(CancellationToken cancellationToken)
    {
        var gridChargingPower = _configuration.GetValue<int>("GRID_CHARGING_POWER");

        var currentDayAheadEnergyPrice = await _dayAheadEnergyPricesRepository.GetCheapestEnergyPriceForTimestamp(DateTime.Now);
        var modbusOverview = await _modbusService.GetBulkOverview();
        var isCharging = modbusOverview.StorageControlMode == StorageControlMode.RemoteControl;
        var chargingTimeRemaining = currentDayAheadEnergyPrice.To - DateTime.Now;
        var shouldCharge = currentDayAheadEnergyPrice.ShouldCharge;
        var shouldStartCharging = false;
        var shouldStopCharging = false;

        if (shouldCharge)
        {
            // If the current battery level is below 90% and there is no
            // high consumption right now, start charging from grid.
            if (modbusOverview.BatteryLevel < 90 && modbusOverview.CurrentConsumptionPower < 1000)
            {
                shouldStartCharging = true;
            }
            else
            {
                shouldStopCharging = true;
            }

            // If the current solar power is higher than the grid charging power, stop charging from grid.
            if (modbusOverview.CurrentSolarPower > gridChargingPower)
            {
                shouldStopCharging = true;
            }
        }
        else if (isCharging)
        {
            shouldStopCharging = true;
        }

        if (shouldStartCharging && !shouldStopCharging)
        {
            _logger.LogInformation("Starting home battery charging for {ChargingTimeRemaining} at {GridChargingPower}W", chargingTimeRemaining, gridChargingPower);
            //await _modbusService.StartChargingBattery(chargingTimeRemaining, gridChargingPower);
        }

        if (shouldStopCharging)
        {
            _logger.LogInformation("Stopping home battery charging");
            //await _modbusService.StopChargingBattery();
        }
    }

    public async Task PrepareCheapestPeriods(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var pricesToday = await _dayAheadEnergyPricesRepository.GetEnergyPriceForDate(today, cancellationToken);
        var pricesTomorrow = await _dayAheadEnergyPricesRepository.GetEnergyPriceForDate(tomorrow, cancellationToken);
        var anyCheapestPricesToday = await _dayAheadEnergyPricesRepository.AnyCheapestEnergyPricesOnDate(today, cancellationToken);
        var anyCheapestPricesTomorrow = await _dayAheadEnergyPricesRepository.AnyCheapestEnergyPricesOnDate(tomorrow, cancellationToken);

        if (pricesToday.Any() && !anyCheapestPricesToday)
        {
            var orderedPrices = pricesToday.OrderBy(x => x.EuroPerMWh).ThenBy(x => x.From);

            var results = new List<DayAheadCheapestEnergyPricesEntry>();
            var order = 0;
            foreach (var price in orderedPrices)
            {
                await _dayAheadEnergyPricesRepository.AddCheapestEnergyPrice(
                    new DayAheadCheapestEnergyPricesEntry
                    {
                        From = price.From,
                        To = price.To,
                        Order = ++order,
                        EuroPerMWh = price.EuroPerMWh
                    });
            }
        }

        if (pricesTomorrow.Any() && !anyCheapestPricesTomorrow)
        {
            var orderedPrices = pricesTomorrow.OrderBy(x => x.EuroPerMWh).ThenBy(x => x.From);

            var results = new List<DayAheadCheapestEnergyPricesEntry>();
            var order = 0;
            foreach (var price in orderedPrices)
            {
                await _dayAheadEnergyPricesRepository.AddCheapestEnergyPrice(
                    new DayAheadCheapestEnergyPricesEntry
                    {
                        From = price.From,
                        To = price.To,
                        Order = ++order,
                        EuroPerMWh = price.EuroPerMWh
                    });
            }
        }
    }

    private async Task<ForecastOverview> GetSolarForecast()
    {
        const decimal LATITUDE = 51.06M;
        const decimal LONGITUDE = 4.36M;
        const byte DAMPING = 1;

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
            Sunset = zw6.Sunset,
            WattHourPeriods = BuildPeriods(zw6.WattHourPeriods, no3.WattHourPeriods, zo4.WattHourPeriods)
        };
    }

    private List<WattHourPeriod> BuildPeriods(List<WattHourPeriod> wattHourPeriods1, List<WattHourPeriod> wattHourPeriods2, List<WattHourPeriod> wattHourPeriods3)
    {
        var result = new List<WattHourPeriod>();

        for (var i = 0; i < wattHourPeriods1.Count; i++)
        {
            result.Add(new WattHourPeriod
            {
                Timestamp = wattHourPeriods1[i].Timestamp,
                WattHours = wattHourPeriods1[i].WattHours + wattHourPeriods2[i].WattHours + wattHourPeriods3[i].WattHours
            });
        }

        return result;
    }
}