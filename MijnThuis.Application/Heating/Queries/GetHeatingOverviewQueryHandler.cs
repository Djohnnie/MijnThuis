using Mapster;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Heating;
using MijnThuis.Integrations.Heating;

namespace MijnThuis.Application.Heating.Queries;

public class GetHeatingOverviewQueryHandler : IRequestHandler<GetHeatingOverviewQuery, GetHeatingOverviewResponse>
{
    private readonly IHeatingService _heatingService;
    private readonly IMemoryCache _memoryCache;

    public GetHeatingOverviewQueryHandler(
        IHeatingService heatingService,
        IMemoryCache memoryCache)
    {
        _heatingService = heatingService;
        _memoryCache = memoryCache;
    }

    public async Task<GetHeatingOverviewResponse> Handle(GetHeatingOverviewQuery request, CancellationToken cancellationToken)
    {
        var heatingResult = await GetOverview();

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