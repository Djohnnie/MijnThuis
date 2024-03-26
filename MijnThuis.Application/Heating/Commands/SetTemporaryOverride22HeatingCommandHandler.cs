using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Heating;
using MijnThuis.Integrations.Heating;

namespace MijnThuis.Application.Heating.Commands;

public class SetTemporaryOverride22HeatingCommandHandler : IRequestHandler<SetTemporaryOverride22HeatingCommand, HeatingCommandResponse>
{
    private readonly IHeatingService _heatingService;
    private readonly IMemoryCache _memoryCache;

    public SetTemporaryOverride22HeatingCommandHandler(
        IHeatingService heatingService,
        IMemoryCache memoryCache)
    {
        _heatingService = heatingService;
        _memoryCache = memoryCache;
    }

    public async Task<HeatingCommandResponse> Handle(SetTemporaryOverride22HeatingCommand request, CancellationToken cancellationToken)
    {
        var heatingResult = await _heatingService.SetTemporaryOverrideHeating(22M);

        if (!heatingResult)
        {
            return new HeatingCommandResponse
            {
                Success = false
            };
        }

        var isManual = false;

        while (!isManual)
        {
            var overviewResult = await _heatingService.GetOverview();
            isManual = overviewResult.Mode == "TemporaryOverride" && overviewResult.Setpoint == 22M;

            if (!isManual)
            {
                await Task.Delay(1000);
            }
        }

        _memoryCache.Remove("HEATING_OVERVIEW");

        return new HeatingCommandResponse
        {
            Success = isManual
        };
    }
}