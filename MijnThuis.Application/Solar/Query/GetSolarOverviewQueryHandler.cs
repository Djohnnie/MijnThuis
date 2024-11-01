using Mapster;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Solar.Query;

public class GetSolarOverviewQueryHandler : IRequestHandler<GetSolarOverviewQuery, GetSolarOverviewResponse>
{
    private readonly ISolarService _solarService;
    private readonly IForecastService _forecastService;
    private readonly IModbusService _modbusService;
    private readonly IMemoryCache _memoryCache;

    public GetSolarOverviewQueryHandler(
        ISolarService solarService,
        IForecastService forecastService,
        IModbusService modbusService,
        IMemoryCache memoryCache)
    {
        _solarService = solarService;
        _forecastService = forecastService;
        _modbusService = modbusService;
        _memoryCache = memoryCache;
    }

    public async Task<GetSolarOverviewResponse> Handle(GetSolarOverviewQuery request, CancellationToken cancellationToken)
    {
        const decimal LATITUDE = 51.06M;
        const decimal LONGITUDE = 4.36M;

        var solarResult = await GetOverview();
        var batteryResult = await GetBatteryLevel();
        var energyResult = await GetEnergy();
        var zw6 = await GetForecast(LATITUDE, LONGITUDE, 39M, 43M, 2.4M);
        var no3 = await GetForecast(LATITUDE, LONGITUDE, 39M, -137M, 1.2M);
        var zo4 = await GetForecast(LATITUDE, LONGITUDE, 10M, -47M, 1.6M);

        var result = solarResult.Adapt<GetSolarOverviewResponse>();
        result.BatteryLevel = (int)Math.Round(batteryResult.Level);
        result.BatteryHealth = (int)Math.Round(batteryResult.Health);
        result.LastDayEnergy = energyResult.LastDayEnergy / 1000M;
        result.LastMonthEnergy = energyResult.LastMonthEnergy / 1000M;
        result.SolarForecastToday = (zw6.EstimatedWattHoursToday + no3.EstimatedWattHoursToday + zo4.EstimatedWattHoursToday) / 1000M;
        result.SolarForecastTomorrow = (zw6.EstimatedWattHoursTomorrow + no3.EstimatedWattHoursTomorrow + zo4.EstimatedWattHoursTomorrow) / 1000M;

        return result;
    }

    private Task<SolarOverview> GetOverview()
    {
        return _solarService.GetOverview();
    }

    private Task<BatteryLevel> GetBatteryLevel()
    {
        return GetCachedValue("SOLAR_BATTERY_LEVEL", _modbusService.GetBatteryLevel, 5);
    }

    private Task<EnergyProduced> GetEnergy()
    {
        return GetCachedValue("SOLAR_ENERGY", _solarService.GetEnergy, 15);
    }

    private Task<ForecastOverview> GetForecast(decimal latitude, decimal longitude, decimal declination, decimal azimuth, decimal power)
    {
        return GetCachedValue($"SOLAR_FORECAST[{latitude}|{longitude}|{declination}|{azimuth}|{power}]", () => _forecastService.GetSolarForecastEstimate(latitude, longitude, declination, azimuth, power), 60);
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