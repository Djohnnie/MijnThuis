﻿using Microsoft.Extensions.DependencyInjection;
using MijnThuis.Integrations.Car;
using MijnThuis.Integrations.Heating;
using MijnThuis.Integrations.Power;
using MijnThuis.Integrations.Sauna;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Integrations.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddTransient<IPowerService, PowerService>();
        services.AddTransient<IWakeOnLanService, WakeOnLanService>();
        services.AddTransient<ISolarService, SolarService>();
        services.AddScoped<IHeatingService, HeatingService>();
        services.AddTransient<ICarService, CarService>();
        services.AddTransient<ISaunaService, SaunaService>();

        return services;
    }
}