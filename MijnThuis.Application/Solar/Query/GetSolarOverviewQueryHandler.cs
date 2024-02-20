using Mapster;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Solar.Query;

public class GetSolarOverviewQueryHandler : IRequestHandler<GetSolarOverviewQuery, GetSolarOverviewResponse>
{
    private readonly ISolarService _solarService;
    private readonly IMemoryCache _memoryCache;

    public GetSolarOverviewQueryHandler(
        ISolarService solarService,
        IMemoryCache memoryCache)
    {
        _solarService = solarService;
        _memoryCache = memoryCache;
    }

    public async Task<GetSolarOverviewResponse> Handle(GetSolarOverviewQuery request, CancellationToken cancellationToken)
    {
        var solarResult = await GetOverview();
        var batteryResult = await GetBatteryLevel();
        var energyResult = await GetEnergy();

        var result = solarResult.Adapt<GetSolarOverviewResponse>();
        result.BatteryHealth = (int)Math.Round(batteryResult.Health);
        result.LastDayEnergy = energyResult.LastDayEnergy / 1000M;
        result.LastMonthEnergy = energyResult.LastMonthEnergy / 1000M;

        return result;
    }

    private Task<SolarOverview> GetOverview()
    {
        return _solarService.GetOverview();
    }

    private Task<BatteryLevel> GetBatteryLevel()
    {
        return GetCachedValue("SOLAR_BATTERY_LEVEL", _solarService.GetBatteryLevel, 60);
    }

    private Task<EnergyProduced> GetEnergy()
    {
        return GetCachedValue("SOLAR_ENERGY", _solarService.GetEnergy, 15);
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