using MediatR;
using MijnThuis.Contracts.Power;
using MijnThuis.Integrations.Power;

namespace MijnThuis.Application.Power.Commands;

public class SetVijverPowerSwitchCommandHandler : IRequestHandler<SetVijverPowerSwitchCommand, SetPowerSwitchResponse>
{
    private readonly IShellyService _shellyService;

    public SetVijverPowerSwitchCommandHandler(IShellyService shellyService)
    {
        _shellyService = shellyService;
    }

    public async Task<SetPowerSwitchResponse> Handle(SetVijverPowerSwitchCommand request, CancellationToken cancellationToken)
    {
        var switchResult = await _shellyService.SetVijverPowerSwitch(request.IsOn);

        if (!switchResult)
        {
            return new SetPowerSwitchResponse
            {
                Success = false
            };
        }

        var isOn = !request.IsOn;

        while (isOn != request.IsOn)
        {
            var overviewResult = await _shellyService.GetVijverPowerSwitchOverview();
            isOn = overviewResult.IsOn;

            if (isOn != request.IsOn)
            {
                await Task.Delay(1000);
            }
        }

        return new SetPowerSwitchResponse
        {
            Success = isOn
        };
    }
}