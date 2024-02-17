using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Sauna;
using MijnThuis.Integrations.Sauna;

namespace MijnThuis.Application.Sauna.Commands;

public class StartSaunaCommandHandler : IRequestHandler<StartSaunaCommand, SaunaCommandResponse>
{
    private readonly ISaunaService _saunaService;
    private readonly IMemoryCache _memoryCache;

    public StartSaunaCommandHandler(
        ISaunaService saunaService,
        IMemoryCache memoryCache)
    {
        _saunaService = saunaService;
        _memoryCache = memoryCache;
    }

    public async Task<SaunaCommandResponse> Handle(StartSaunaCommand request, CancellationToken cancellationToken)
    {
        var activeSessionId = await _saunaService.GetActiveSession();

        if (activeSessionId == null)
        {
            var result = await _saunaService.StartSauna();

            if (!result)
            {
                return new SaunaCommandResponse
                {
                    Success = false
                };
            }

            var isSauna = false;

            while (!isSauna)
            {
                var overviewResult = await _saunaService.GetState();
                isSauna = overviewResult == "Sauna";

                if (!isSauna)
                {
                    await Task.Delay(1000);
                }
            }

            _memoryCache.Remove("SAUNA_STATE");

            return new SaunaCommandResponse
            {
                Success = isSauna
            };
        }

        return new SaunaCommandResponse
        {
            Success = false
        };
    }
}