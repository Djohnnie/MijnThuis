using MediatR;
using MijnThuis.Contracts.Heating;
using MijnThuis.Integrations.Heating;

namespace MijnThuis.Application.Heating.Commands;

public class SetScheduledHeatingCommandHandler : IRequestHandler<SetScheduledHeatingCommand, HeatingCommandResponse>
{
    private readonly IHeatingService _heatingService;

    public SetScheduledHeatingCommandHandler(IHeatingService heatingService)
    {
        _heatingService = heatingService;
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

        return new HeatingCommandResponse
        {
            Success = isScheduled
        };
    }
}