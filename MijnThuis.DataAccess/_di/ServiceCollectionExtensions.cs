using Microsoft.Extensions.DependencyInjection;
using MijnThuis.DataAccess.Repositories;

namespace MijnThuis.DataAccess.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<MijnThuisDbContext>();
        services.AddScoped<IFlagRepository, FlagRepository>();
        services.AddScoped<ICarChargingEnergyHistoryRepository, CarChargingEnergyHistoryRepository>();
        services.AddScoped<IDayAheadEnergyPricesRepository, DayAheadEnergyPricesRepository>();

        return services;
    }
}