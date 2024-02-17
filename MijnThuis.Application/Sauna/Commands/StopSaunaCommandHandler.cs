using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Sauna;
using MijnThuis.Integrations.Sauna;

namespace MijnThuis.Application.Sauna.Commands;

public class StopSaunaCommandHandler : IRequestHandler<StopSaunaCommand, SaunaCommandResponse>
{
    private readonly ISaunaService _saunaService;
    private readonly IMemoryCache _memoryCache;

    public StopSaunaCommandHandler(
        ISaunaService saunaService,
        IMemoryCache memoryCache)
    {
        _saunaService = saunaService;
        _memoryCache = memoryCache;
    }

    public async Task<SaunaCommandResponse> Handle(StopSaunaCommand request, CancellationToken cancellationToken)
    {
        var activeSessionId = await _saunaService.GetActiveSession();

        if (activeSessionId != null)
        {
            var result = await _saunaService.StopSauna(activeSessionId);

            if (!result)
            {
                return new SaunaCommandResponse
                {
                    Success = false
                };
            }

            var isOff = false;

            while (!isOff)
            {
                var overviewResult = await _saunaService.GetState();
                isOff = overviewResult == "Uit";

                if (!isOff)
                {
                    await Task.Delay(1000);
                }
            }

            _memoryCache.Remove("SAUNA_STATE");

            return new SaunaCommandResponse
            {
                Success = isOff
            };
        }

        return new SaunaCommandResponse
        {
            Success = true
        };
    }
}