using MediatR;
using MijnThuis.Contracts.Heating;
using MijnThuis.Integrations.Heating;

namespace MijnThuis.Application.Heating.Commands;

public class SetManual16HeatingCommandHandler : IRequestHandler<SetManual16HeatingCommand, HeatingCommandResponse>
{
    private readonly IHeatingService _heatingService;

    public SetManual16HeatingCommandHandler(IHeatingService heatingService)
    {
        _heatingService = heatingService;
    }

    public async Task<HeatingCommandResponse> Handle(SetManual16HeatingCommand request, CancellationToken cancellationToken)
    {
        var heatingResult = await _heatingService.SetManualHeating(16M);

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
            isManual = overviewResult.Mode == "Manual" && overviewResult.Setpoint == 16M;

            if (!isManual)
            {
                await Task.Delay(1000);
            }
        }

        return new HeatingCommandResponse
        {
            Success = isManual
        };
    }
}