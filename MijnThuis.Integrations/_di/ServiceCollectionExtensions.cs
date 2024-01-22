using Microsoft.Extensions.DependencyInjection;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Integrations.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrations(this IServiceCollection services)
    {
        services.AddTransient<ICarService, CarService>();

        return services;
    }
}