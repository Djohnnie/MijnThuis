using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Airconditioning;
using MijnThuis.Integrations.Airconditioning;

namespace MijnThuis.Application.Airconditioning.Commands;

public class DecreaseTargetTemperatureAirconditioningCommandHandler : IRequestHandler<DecreaseTargetTemperatureAirconditioningCommand, AirconditioningCommandResponse>
{
    private readonly IAirconditioningService _airconditioningService;
    private readonly IMemoryCache _memoryCache;

    public DecreaseTargetTemperatureAirconditioningCommandHandler(
        IAirconditioningService airconditioningService,
        IMemoryCache memoryCache)
    {
        _airconditioningService = airconditioningService;
        _memoryCache = memoryCache;
    }

    public async Task<AirconditioningCommandResponse> Handle(DecreaseTargetTemperatureAirconditioningCommand request, CancellationToken cancellationToken)
    {
        var result = await _airconditioningService.DecreaseTargetTemperature();

        _memoryCache.Remove("AIRCONDITIONING_OVERVIEW");

        return new AirconditioningCommandResponse
        {
            Success = result
        };
    }
}
