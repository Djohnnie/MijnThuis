using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Heating;
using MijnThuis.Integrations.Heating;

namespace MijnThuis.Application.Heating.Commands;

public class SetScheduledHeatingCommandHandler : IRequestHandler<SetScheduledHeatingCommand, HeatingCommandResponse>
{
    private readonly IHeatingService _heatingService;
    private readonly IMemoryCache _memoryCache;

    public SetScheduledHeatingCommandHandler(
        IHeatingService heatingService,
        IMemoryCache memoryCache)
    {
        _heatingService = heatingService;
        _memoryCache = memoryCache;
    }

    public async Task<HeatingCommandResponse> Handle(SetScheduledHeatingCommand request, CancellationToken cancellationToken)
    {
        var heatingResult = await _heatingService.SetScheduledHeating();

        if (!heatingResult)
        {
            return new HeatingCommandResponse
            {
                Success = false
            };
        }

        var isScheduled = false;

        while (!isScheduled)
        {
            var overviewResult = await _heatingService.GetOverview();
            isScheduled = overviewResult.Mode == "Scheduling";

            if (!isScheduled)
            {
                await Task.Delay(1000);
            }
        }

        _memoryCache.Remove("HEATING_OVERVIEW");

        return new HeatingCommandResponse
        {
            Success = isScheduled
        };
    }
}