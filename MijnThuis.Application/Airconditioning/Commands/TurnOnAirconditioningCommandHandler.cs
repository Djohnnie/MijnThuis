using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Airconditioning;
using MijnThuis.Integrations.Airconditioning;

namespace MijnThuis.Application.Airconditioning.Commands;

public class TurnOnAirconditioningCommandHandler : IRequestHandler<TurnOnAirconditioningCommand, AirconditioningCommandResponse>
{
    private readonly IAirconditioningService _airconditioningService;
    private readonly IMemoryCache _memoryCache;

    public TurnOnAirconditioningCommandHandler(
        IAirconditioningService airconditioningService,
        IMemoryCache memoryCache)
    {
        _airconditioningService = airconditioningService;
        _memoryCache = memoryCache;
    }

    public async Task<AirconditioningCommandResponse> Handle(TurnOnAirconditioningCommand request, CancellationToken cancellationToken)
    {
        var result = await _airconditioningService.TurnOn();

        _memoryCache.Remove("AIRCONDITIONING_OVERVIEW");

        return new AirconditioningCommandResponse
        {
            Success = result
        };
    }
}
