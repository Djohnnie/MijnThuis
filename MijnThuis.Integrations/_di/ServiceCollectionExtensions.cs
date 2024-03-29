﻿using Microsoft.Extensions.DependencyInjection;
using MijnThuis.Integrations.Car;
using MijnThuis.Integrations.Heating;
using MijnThuis.Integrations.Lamps;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Sauna;
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
        services.AddScoped<IHeatingService, HeatingService>();
        services.AddScoped<ILampsService, LampsService>();
        services.AddTransient<ICarService, CarService>();
        services.AddTransient<IChargerService, ChargerService>();
        services.AddTransient<ISaunaService, SaunaService>();

        return services;
    }
}