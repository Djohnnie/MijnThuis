using Microsoft.Extensions.DependencyInjection;
using MijnThuis.Integrations.Car;
using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Heating;
using MijnThuis.Integrations.Lamps;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Sauna;
using MijnThuis.Integrations.SmartLock;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Integrations.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddTransient<IPowerService, PowerService>();
        services.AddTransient<IShellyService, ShellyService>();
        services.AddTransient<IWakeOnLanService, WakeOnLanService>();
        services.AddScoped<ISolarService, SolarService>();
        services.AddScoped<IModbusService, ModbusService>();
        services.AddScoped<IHeatingService, HeatingService>();
        services.AddScoped<ILampsService, LampsService>();
        services.AddTransient<ICarService, CarService>();
        services.AddTransient<IChargerService, ChargerService>();
        services.AddTransient<ISaunaService, SaunaService>();
        services.AddTransient<IForecastService, ForecastService>();
        services.AddTransient<ISmartLockService, SmartLockService>();
        services.AddTransient<IEnergyPricesService, EnergyPricesService>();

        return services;
    }
}