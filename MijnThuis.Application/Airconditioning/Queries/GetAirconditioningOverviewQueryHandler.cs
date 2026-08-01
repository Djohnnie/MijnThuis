using Mapster;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Airconditioning;
using MijnThuis.Integrations.Airconditioning;

namespace MijnThuis.Application.Airconditioning.Queries;

public class GetAirconditioningOverviewQueryHandler : IRequestHandler<GetAirconditioningOverviewQuery, GetAirconditioningOverviewResponse>
{
    private readonly IAirconditioningService _airconditioningService;
    private readonly IMemoryCache _memoryCache;

    public GetAirconditioningOverviewQueryHandler(
        IAirconditioningService airconditioningService,
        IMemoryCache memoryCache)
    {
        _airconditioningService = airconditioningService;
        _memoryCache = memoryCache;
    }

    public async Task<GetAirconditioningOverviewResponse> Handle(GetAirconditioningOverviewQuery request, CancellationToken cancellationToken)
    {
        var overview = await GetOverview();

        return overview.Adapt<GetAirconditioningOverviewResponse>();
    }

    private Task<AirconditioningOverview> GetOverview()
    {
        return GetCachedValue("AIRCONDITIONING_OVERVIEW", _airconditioningService.GetOverview, 1);
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
