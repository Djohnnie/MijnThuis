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

        var result = solarResult.Adapt<GetSolarOverviewResponse>();
        result.BatteryLevel = (int)Math.Round(batteryResult.Level);
        result.BatteryHealth = (int)Math.Round(batteryResult.Health);

        return result;
    }

    private Task<SolarOverview> GetOverview()
    {
        return GetCachedValue("SOLAR_OVERVIEW", _solarService.GetOverview, 5);
    }

    private Task<BatteryLevel> GetBatteryLevel()
    {
        return GetCachedValue("SOLAR_BATTERY_LEVEL", _solarService.GetBatteryLevel, 5);
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