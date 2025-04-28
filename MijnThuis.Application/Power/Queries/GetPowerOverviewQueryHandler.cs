using Mapster;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Power;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Power.Queries;

public class GetPowerOverviewQueryHandler : IRequestHandler<GetPowerOverviewQuery, GetPowerOverviewResponse>
{
    private readonly IPowerService _powerService;
    private readonly IShellyService _shellyService;
    private readonly ISolarService _solarService;
    private readonly IMemoryCache _memoryCache;

    public GetPowerOverviewQueryHandler(
        IPowerService powerService,
        IShellyService shellyService,
        ISolarService solarService,
        IMemoryCache memoryCache)
    {
        _powerService = powerService;
        _shellyService = shellyService;
        _solarService = solarService;
        _memoryCache = memoryCache;
    }

    public async Task<GetPowerOverviewResponse> Handle(GetPowerOverviewQuery request, CancellationToken cancellationToken)
    {
        var powerResult = await _powerService.GetOverview();
        var consumptionResult = await _solarService.GetOverview();
        var energyTodayResult = await GetEnergyToday();
        var energyThisMonthResult = await GetEnergyThisMonth();
        var tvPowerSwitchOverview = await _shellyService.GetTvPowerSwitchOverview();
        var bureauPowerSwitchOverview = await _shellyService.GetBureauPowerSwitchOverview();
        var vijverPowerSwitchOverview = await _shellyService.GetVijverPowerSwitchOverview();

        var result = powerResult.Adapt<GetPowerOverviewResponse>();
        result.CurrentConsumption = consumptionResult.CurrentConsumptionPower;
        result.EnergyToday = energyTodayResult.Purchased / 1000M;
        result.EnergyThisMonth = energyThisMonthResult.Purchased / 1000M;
        result.IsTvOn = tvPowerSwitchOverview.IsOn;
        result.IsBureauOn = bureauPowerSwitchOverview.IsOn;
        result.IsVijverOn = vijverPowerSwitchOverview.IsOn;

        return result;
    }

    private Task<EnergyOverview> GetEnergyToday()
    {
        return GetCachedValue("ENERGY_TODAY", _solarService.GetEnergyToday, 15);
    }

    private Task<EnergyOverview> GetEnergyThisMonth()
    {
        return GetCachedValue("ENERGY_THIS_MONTH", _solarService.GetEnergyThisMonth, 15);
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