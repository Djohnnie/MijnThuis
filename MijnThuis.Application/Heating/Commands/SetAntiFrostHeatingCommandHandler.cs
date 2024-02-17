using MediatR;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.Contracts.Heating;
using MijnThuis.Integrations.Heating;

namespace MijnThuis.Application.Heating.Commands;

public class SetAntiFrostHeatingCommandHandler : IRequestHandler<SetAntiFrostHeatingCommand, HeatingCommandResponse>
{
    private readonly IHeatingService _heatingService;
    private readonly IMemoryCache _memoryCache;

    public SetAntiFrostHeatingCommandHandler(
        IHeatingService heatingService,
        IMemoryCache memoryCache)
    {
        _heatingService = heatingService;
        _memoryCache = memoryCache;
    }

    public async Task<HeatingCommandResponse> Handle(SetAntiFrostHeatingCommand request, CancellationToken cancellationToken)
    {
        var heatingResult = await _heatingService.SetAntiFrostHeating();

        if (!heatingResult)
        {
            return new HeatingCommandResponse
            {
                Success = false
            };
        }

        var isAntiFrost = false;

        while (!isAntiFrost)
        {
            var overviewResult = await _heatingService.GetOverview();
            isAntiFrost = overviewResult.Mode == "FrostProtection";

            if (!isAntiFrost)
            {
                await Task.Delay(1000);
            }
        }

        _memoryCache.Remove("HEATING_OVERVIEW");

        return new HeatingCommandResponse
        {
            Success = isAntiFrost
        };
    }
}