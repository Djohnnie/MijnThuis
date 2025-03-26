using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Heating;
using MijnThuis.DataAccess;
using MijnThuis.Integrations.Heating;

namespace MijnThuis.Application.Heating.Queries;

public class GetHeatingOverviewQueryHandler : IRequestHandler<GetHeatingOverviewQuery, GetHeatingOverviewResponse>
{
    private readonly MijnThuisDbContext _dbContext;
    private readonly IHeatingService _heatingService;
    private readonly IMemoryCache _memoryCache;

    public GetHeatingOverviewQueryHandler(
        MijnThuisDbContext dbContext,
        IHeatingService heatingService,
        IMemoryCache memoryCache)
    {
        _dbContext = dbContext;
        _heatingService = heatingService;
        _memoryCache = memoryCache;
    }

    public async Task<GetHeatingOverviewResponse> Handle(GetHeatingOverviewQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var thisMonth = new DateTime(today.Year, today.Month, 1);

        var heatingResult = await GetOverview();
        var energyHistory = await _dbContext.EnergyHistory
            .Where(x => x.Date.Date >= thisMonth && x.Date.Date <= today)
            .ToListAsync();

        var result = heatingResult.Adapt<GetHeatingOverviewResponse>();
        result.Mode = heatingResult.Mode switch
        {
            "Preheat" => "Voorverwarmen",
            "Manual" => "Handmatig",
            "Scheduling" => "Schema",
            "FrostProtection" => "Uit/Vorstbeveiliging",
            "TemporaryOverride" => "Tijdelijke overname",
            _ => "Uit"
        };

        result.GasUsageToday = energyHistory.Where(x => x.Date.Date == today).Sum(x => x.TotalGasDelta);
        result.GasUsageTodayKwh = energyHistory.Where(x => x.Date.Date == today).Sum(x => x.TotalGasKwhDelta);
        result.GasUsageThisMonth = energyHistory.Where(x => x.Date.Date.Year == today.Year && x.Date.Date.Month == today.Month).Sum(x => x.TotalGasDelta);
        result.GasUsageThisMonthKwh = energyHistory.Where(x => x.Date.Date.Year == today.Year && x.Date.Date.Month == today.Month).Sum(x => x.TotalGasKwhDelta);

        return result;
    }

    private Task<HeatingOverview> GetOverview()
    {
        return GetCachedValue("HEATING_OVERVIEW", _heatingService.GetOverview, 5);
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