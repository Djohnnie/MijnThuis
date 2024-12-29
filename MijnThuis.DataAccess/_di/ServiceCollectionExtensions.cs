using Microsoft.Extensions.DependencyInjection;

namespace MijnThuis.DataAccess.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<MijnThuisDbContext>();

        return services;
    }
}