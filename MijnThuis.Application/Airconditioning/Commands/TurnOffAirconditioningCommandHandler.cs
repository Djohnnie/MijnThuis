using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Airconditioning;
using MijnThuis.Integrations.Airconditioning;

namespace MijnThuis.Application.Airconditioning.Commands;

public class TurnOffAirconditioningCommandHandler : IRequestHandler<TurnOffAirconditioningCommand, AirconditioningCommandResponse>
{
    private readonly IAirconditioningService _airconditioningService;
    private readonly IMemoryCache _memoryCache;

    public TurnOffAirconditioningCommandHandler(
        IAirconditioningService airconditioningService,
        IMemoryCache memoryCache)
    {
        _airconditioningService = airconditioningService;
        _memoryCache = memoryCache;
    }

    public async Task<AirconditioningCommandResponse> Handle(TurnOffAirconditioningCommand request, CancellationToken cancellationToken)
    {
        var result = await _airconditioningService.TurnOff();

        _memoryCache.Remove("AIRCONDITIONING_OVERVIEW");

        return new AirconditioningCommandResponse
        {
            Success = result
        };
    }
}
