using Mapster;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Power.Queries;

public class GetPowerOverviewQueryHandler : IRequestHandler<GetPowerOverviewQuery, GetPowerOverviewResponse>
{
    private readonly IPowerService _powerService;
    private readonly IShellyService _shellyService;
    private readonly ISolarService _solarService;
    private readonly IModbusService _modbusService;
    private readonly IDayAheadEnergyPricesRepository _energyPricesRepository;
    private readonly IMemoryCache _memoryCache;

    public GetPowerOverviewQueryHandler(
        IPowerService powerService,
        IShellyService shellyService,
        ISolarService solarService,
        IModbusService modbusService,
        IDayAheadEnergyPricesRepository energyPricesRepository,
        IMemoryCache memoryCache)
    {
        _powerService = powerService;
        _shellyService = shellyService;
        _solarService = solarService;
        _modbusService = modbusService;
        _energyPricesRepository = energyPricesRepository;
        _memoryCache = memoryCache;
    }

    public async Task<GetPowerOverviewResponse> Handle(GetPowerOverviewQuery request, CancellationToken cancellationToken)
    {
        var powerResult = await _powerService.GetOverview();
        var consumptionResult = await _modbusService.GetOverview();
        var energyTodayResult = await GetEnergyToday();
        var energyThisMonthResult = await GetEnergyThisMonth();
        var energyPricing = await _energyPricesRepository.GetEnergyPriceForTimestamp(DateTime.Now);
        var tvPowerSwitchOverview = await _shellyService.GetTvPowerSwitchOverview();
        var bureauPowerSwitchOverview = await _shellyService.GetBureauPowerSwitchOverview();
        var vijverPowerSwitchOverview = await _shellyService.GetVijverPowerSwitchOverview();

        var result = powerResult.Adapt<GetPowerOverviewResponse>();
        result.CurrentConsumption = consumptionResult.CurrentConsumptionPower / 1000M;
        result.EnergyToday = energyTodayResult.Purchased / 1000M;
        result.EnergyThisMonth = energyThisMonthResult.Purchased / 1000M;
        result.CurrentPricePeriod = $"({energyPricing.From:HHu} - {energyPricing.To.AddSeconds(1):HHu})";
        result.CurrentConsumptionPrice = energyPricing.ConsumptionCentsPerKWh;
        result.CurrentInjectionPrice = energyPricing.InjectionCentsPerKWh;
        result.IsTvOn = tvPowerSwitchOverview.IsOn;
        result.IsBureauOn = bureauPowerSwitchOverview.IsOn;
        result.IsVijverOn = vijverPowerSwitchOverview.IsOn;

        return result;
    }

    private async Task<EnergyOverview> GetEnergyToday()
    {
        try
        {
            return await GetCachedValue("ENERGY_TODAY", _solarService.GetEnergyToday, 15);
        }
        catch
        {
            return new EnergyOverview();
        }
    }

    private async Task<EnergyOverview> GetEnergyThisMonth()
    {
        try
        {
            return await GetCachedValue("ENERGY_THIS_MONTH", _solarService.GetEnergyThisMonth, 15);
        }
        catch
        {
            return new EnergyOverview();
        }
    }

    private async Task<T> GetCachedValue<T>(string key, Func<Task<T>> valueFactory, int absoluteExpiration)
    {
        if (_memoryCache.TryGetValue(key, out T value))
        {
            return value;
        }

        value = await valueFactory();
        _memoryCache.Set(key, value, TimeSpan.FromMinutes(absoluteExpiration));

        return value;
    }
}