using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Solar.Queries;

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
        const byte DAMPING = 1;

        var solarResult = await GetOverview();
        var batteryResult = await GetBatteryLevel();
        var energyResult = await GetEnergy();
        var zw6 = await GetForecast(LATITUDE, LONGITUDE, 28M, 43M, 2.4M, DAMPING);
        var no3 = await GetForecast(LATITUDE, LONGITUDE, 33M, -137M, 1.2M, DAMPING);
        var zo4 = await GetForecast(LATITUDE, LONGITUDE, 12M, -47M, 1.6M, DAMPING);

        var result = new GetSolarOverviewResponse();
        result.CurrentSolarPower = solarResult.CurrentSolarPower / 1000M;
        result.CurrentBatteryPower = solarResult.CurrentBatteryPower / 1000M;
        result.CurrentGridPower = -solarResult.CurrentGridPower / 1000M;
        result.CurrentConsumptionPower = solarResult.CurrentConsumptionPower / 1000M;
        result.BatteryLevel = (int)Math.Round(batteryResult.Level);
        result.BatteryHealth = (int)Math.Round(batteryResult.Health);
        result.BatteryMaxEnergy = (int)Math.Round(batteryResult.MaxEnergy);
        result.LastDayEnergy = energyResult.LastDayEnergy / 1000M;
        result.LastMonthEnergy = energyResult.LastMonthEnergy / 1000M;
        result.SolarForecastToday = (zw6.EstimatedWattHoursToday + no3.EstimatedWattHoursToday + zo4.EstimatedWattHoursToday) / 1000M;
        result.SolarForecastTomorrow = (zw6.EstimatedWattHoursTomorrow + no3.EstimatedWattHoursTomorrow + zo4.EstimatedWattHoursTomorrow) / 1000M;

        return result;
    }

    private Task<SolarOverview> GetOverview()
    {
        return _modbusService.GetOverview();
    }

    private Task<BatteryLevel> GetBatteryLevel()
    {
        return _modbusService.GetBatteryLevel();
    }

    private Task<EnergyProduced> GetEnergy()
    {
        return GetCachedValue("SOLAR_ENERGY", _solarService.GetEnergy, 15);
    }

    private Task<ForecastOverview> GetForecast(decimal latitude, decimal longitude, decimal declination, decimal azimuth, decimal power, byte damping)
    {
        return GetCachedValue($"SOLAR_FORECAST[{latitude}|{longitude}|{declination}|{azimuth}|{power}]", () => _forecastService.GetSolarForecastEstimate(latitude, longitude, declination, azimuth, power, damping), 60);
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