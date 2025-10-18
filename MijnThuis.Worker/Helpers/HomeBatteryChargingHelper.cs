using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Power;
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
    private readonly IPowerService _powerService;
    private readonly IConfiguration _configuration;
    private readonly IDayAheadEnergyPricesRepository _dayAheadEnergyPricesRepository;
    private readonly ISolarPowerHistoryRepository _solarPowerHistoryRepository;
    private readonly ILogger<HomeBatteryChargingHelper> _logger;

    public HomeBatteryChargingHelper(
        IForecastService forecastService,
        IModbusService modbusService,
        IPowerService powerService,
        IConfiguration configuration,
        IDayAheadEnergyPricesRepository dayAheadEnergyPricesRepository,
        ISolarPowerHistoryRepository solarPowerHistoryRepository,
        ILogger<HomeBatteryChargingHelper> logger)
    {
        _forecastService = forecastService;
        _modbusService = modbusService;
        _powerService = powerService;
        _configuration = configuration;
        _dayAheadEnergyPricesRepository = dayAheadEnergyPricesRepository;
        _solarPowerHistoryRepository = solarPowerHistoryRepository;
        _logger = logger;
    }

    public async Task UpdateChargingSchedule(CancellationToken cancellationToken)
    {
        var gridChargingPower = _configuration.GetValue<int>("GRID_CHARGING_POWER");

        var solarForecast = await GetSolarForecast();
        var batteryLevel = await _modbusService.GetBatteryLevel();

        // Start scheduling the next day from 6 PM today.
        var dayToSchedule = DateTime.Now.Hour < 18 ? DateTime.Today : DateTime.Today.AddDays(1);
        var calculatedSolarForecastOnDayToSchedule = solarForecast.WattHourPeriods
            .Where(x => x.Timestamp.Date == dayToSchedule)
            .Sum(x => x.WattHours);
        var cheapestPricesToday = await _dayAheadEnergyPricesRepository.GetCheapestEnergyPriceForDate(dayToSchedule, cancellationToken);
        var averageDailyEnergy = await _solarPowerHistoryRepository.GetAverageDailyConsumption(dayToSchedule.AddDays(-7), dayToSchedule, cancellationToken);
        var batteryEnergyToCharge = 97 * (100 - batteryLevel.Level);
        var totalEnergyNeeded = (int)averageDailyEnergy + (int)batteryEnergyToCharge - calculatedSolarForecastOnDayToSchedule;

        var numberOf15MinuteBlocksNeeded = (int)(totalEnergyNeeded / (gridChargingPower / 4M));
        var numberOf15MinuteBlocksMarked = 0;
        for (var i = 0; i < cheapestPricesToday.Count; i++)
        {
            if (cheapestPricesToday[i].From > DateTime.Now)
            {
                await _dayAheadEnergyPricesRepository.SetCheapestEnergyPriceShouldCharge(cheapestPricesToday[i].Id, numberOf15MinuteBlocksMarked < numberOf15MinuteBlocksNeeded);
                numberOf15MinuteBlocksMarked++;
            }
        }
    }

    public async Task CheckForBatteryCharging(CancellationToken cancellationToken)
    {
        var gridChargingPower = _configuration.GetValue<int>("GRID_CHARGING_POWER");

        var currentDayAheadEnergyPrice = await _dayAheadEnergyPricesRepository.GetCheapestEnergyPriceForTimestamp(DateTime.Now);
        var chargingTimeRemaining = currentDayAheadEnergyPrice.To - DateTime.Now;
        var shouldCharge = currentDayAheadEnergyPrice.ShouldCharge;
        var isCharging = await _modbusService.IsNotMaxSelfConsumption();
        if (shouldCharge)
        {
            var batteryLevel = await _modbusService.GetBatteryLevel();
            if (!isCharging && batteryLevel.Level < 95)
            {
                _logger.LogInformation("Starting home battery charging for {ChargingTimeRemaining} at {GridChargingPower}W", chargingTimeRemaining, gridChargingPower);
                await _modbusService.StartChargingBattery(chargingTimeRemaining, gridChargingPower);
            }
        }
        else
        {
            if (isCharging)
            {
                _logger.LogInformation("Stopping home battery charging");
                await _modbusService.StopChargingBattery();
            }
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