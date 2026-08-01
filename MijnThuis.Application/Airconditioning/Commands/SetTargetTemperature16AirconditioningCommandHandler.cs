using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Airconditioning;
using MijnThuis.Integrations.Airconditioning;

namespace MijnThuis.Application.Airconditioning.Commands;

public class SetTargetTemperature16AirconditioningCommandHandler : IRequestHandler<SetTargetTemperature16AirconditioningCommand, AirconditioningCommandResponse>
{
    private readonly IAirconditioningService _airconditioningService;
    private readonly IMemoryCache _memoryCache;

    public SetTargetTemperature16AirconditioningCommandHandler(
        IAirconditioningService airconditioningService,
        IMemoryCache memoryCache)
    {
        _airconditioningService = airconditioningService;
        _memoryCache = memoryCache;
    }

    public async Task<AirconditioningCommandResponse> Handle(SetTargetTemperature16AirconditioningCommand request, CancellationToken cancellationToken)
    {
        var result = await _airconditioningService.SetTargetTemperature(16);

        _memoryCache.Remove("AIRCONDITIONING_OVERVIEW");

        return new AirconditioningCommandResponse
        {
            Success = result
        };
    }
}
