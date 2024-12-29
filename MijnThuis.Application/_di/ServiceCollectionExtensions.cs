using Microsoft.Extensions.DependencyInjection;
using MijnThuis.DataAccess.DependencyInjection;
using MijnThuis.Integrations.DependencyInjection;
using System.Reflection;

namespace MijnThuis.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(c => c.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddIntegrations();
        services.AddDataAccess();

        return services;
    }
}