using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Worker.Helpers;

public interface IHomeBatteryChargingHelper
{
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
    private readonly ILogger<HomeBatteryChargingHelper> _logger;

    public HomeBatteryChargingHelper(
        IForecastService forecastService,
        IModbusService modbusService,
        IPowerService powerService,
        IConfiguration configuration,
        IDayAheadEnergyPricesRepository dayAheadEnergyPricesRepository,
        ILogger<HomeBatteryChargingHelper> logger)
    {
        _forecastService = forecastService;
        _modbusService = modbusService;
        _powerService = powerService;
        _configuration = configuration;
        _dayAheadEnergyPricesRepository = dayAheadEnergyPricesRepository;
        _logger = logger;
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
        var date = DateTime.Today;

        var prices = await _dayAheadEnergyPricesRepository.GetEnergyPriceForDate(date, cancellationToken);
        var anyCheapestPrices = await _dayAheadEnergyPricesRepository.AnyCheapestEnergyPricesOnDate(date, cancellationToken);

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
}