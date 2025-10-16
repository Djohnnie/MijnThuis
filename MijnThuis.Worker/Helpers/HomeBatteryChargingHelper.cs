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

        var today = DateTime.Today;
        var cheapestPricesToday = await _dayAheadEnergyPricesRepository.GetCheapestEnergyPriceForDate(today, cancellationToken);
        var averageDailyEnergy = await _solarPowerHistoryRepository.GetAverageDailyConsumption(DateTime.Today.AddDays(-7), DateTime.Today, cancellationToken);

        // Calculate the total amount of 15-minute blocks needed to charge the battery
        // based on the grid charging power, the remaining battery level and the solar forecast.

        var numberOf15MinuteBlocksNeeded = 0;
        for (var i = 0; i < cheapestPricesToday.Count; i++)
        {
            if (i < numberOf15MinuteBlocksNeeded)
            {
                cheapestPricesToday[i].ShouldCharge = true;
            }
        }
    }

    public async Task CheckForBatteryCharging(CancellationToken cancellationToken)
    {
        var gridChargingPower = _configuration.GetValue<int>("GRID_CHARGING_POWER");

        var currentDayAheadEnergyPrice = await _dayAheadEnergyPricesRepository.GetCheapestEnergyPriceForTimestamp(DateTime.Now);
        var chargingTimeRemaining = currentDayAheadEnergyPrice.To - DateTime.Now;
        var shouldCharge = currentDayAheadEnergyPrice.ShouldCharge;
        var isNotCharging = await _modbusService.IsNotChargingInRemoteControlMode();
        if (shouldCharge && isNotCharging)
        {
            await _modbusService.StartChargingBattery(chargingTimeRemaining, gridChargingPower);
        }
    }

    public async Task PrepareCheapestPeriods(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;

        var prices = await _dayAheadEnergyPricesRepository.GetEnergyPriceForDate(today, cancellationToken);
        var anyCheapestPrices = await _dayAheadEnergyPricesRepository.AnyCheapestEnergyPricesOnDate(today, cancellationToken);

        if (prices.Any() && !anyCheapestPrices)
        {
            var orderedPrices = prices.OrderBy(x => x.EuroPerMWh).ThenBy(x => x.From);

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
            //WattHourPeriods = 
        };
    }
}