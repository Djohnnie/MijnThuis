using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Sauna;
using MijnThuis.Integrations.Sauna;

namespace MijnThuis.Application.Sauna.Commands;

public class StartInfraredCommandHandler : IRequestHandler<StartInfraredCommand, SaunaCommandResponse>
{
    private readonly ISaunaService _saunaService;
    private readonly IMemoryCache _memoryCache;

    public StartInfraredCommandHandler(
        ISaunaService saunaService,
        IMemoryCache memoryCache)
    {
        _saunaService = saunaService;
        _memoryCache = memoryCache;
    }

    public async Task<SaunaCommandResponse> Handle(StartInfraredCommand request, CancellationToken cancellationToken)
    {
        var activeSessionId = await _saunaService.GetActiveSession();

        if (activeSessionId == null)
        {
            var result = await _saunaService.StartInfrared();

            if (!result)
            {
                return new SaunaCommandResponse
                {
                    Success = false
                };
            }

            var isInfrared = false;

            while (!isInfrared)
            {
                var overviewResult = await _saunaService.GetState();
                isInfrared = overviewResult == "Infrarood";

                if (!isInfrared)
                {
                    await Task.Delay(1000);
                }
            }

            _memoryCache.Remove("SAUNA_STATE");

            return new SaunaCommandResponse
            {
                Success = isInfrared
            };
        }

        return new SaunaCommandResponse
        {
            Success = false
        };
    }
}