using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Sauna;
using MijnThuis.Integrations.Sauna;

namespace MijnThuis.Application.Sauna.Query;

public class GetSaunaOverviewQueryHandler : IRequestHandler<GetSaunaOverviewQuery, GetSaunaOverviewResponse>
{
    private readonly ISaunaService _saunaService;
    private readonly IMemoryCache _memoryCache;

    public GetSaunaOverviewQueryHandler(
        ISaunaService saunaService,
        IMemoryCache memoryCache)
    {
        _saunaService = saunaService;
        _memoryCache = memoryCache;
    }

    public async Task<GetSaunaOverviewResponse> Handle(GetSaunaOverviewQuery request, CancellationToken cancellationToken)
    {
        var state = await _saunaService.GetState();
        var insideTemperature = await _saunaService.GetInsideTemperature();
        var outsideTemperature = await _saunaService.GetOutsideTemperature();
        var power = await _saunaService.GetPowerUsage();

        return new GetSaunaOverviewResponse
        {
            State = state,
            InsideTemperature = insideTemperature,
            OutsideTemperature = outsideTemperature,
            Power = power
        };
    }

    private Task<string> GetState()
    {
        return GetCachedValue("HEATING_OVERVIEW", _saunaService.GetState, 1);
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