using MediatR;
using MijnThuis.Contracts.Heating;
using MijnThuis.Integrations.Heating;

namespace MijnThuis.Application.Heating.Commands;

public class SetAntiFrostHeatingCommandHandler : IRequestHandler<SetAntiFrostHeatingCommand, HeatingCommandResponse>
{
    private readonly IHeatingService _heatingService;

    public SetAntiFrostHeatingCommandHandler(IHeatingService heatingService)
    {
        _heatingService = heatingService;
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

        return new HeatingCommandResponse
        {
            Success = isAntiFrost
        };
    }
}