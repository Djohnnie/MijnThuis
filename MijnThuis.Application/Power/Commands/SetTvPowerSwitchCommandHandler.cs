using MediatR;
using MijnThuis.Contracts.Power;
using MijnThuis.Integrations.Power;

namespace MijnThuis.Application.Power.Commands;

public class SetTvPowerSwitchCommandHandler : IRequestHandler<SetTvPowerSwitchCommand, SetPowerSwitchResponse>
{
    private readonly IShellyService _shellyService;

    public SetTvPowerSwitchCommandHandler(IShellyService shellyService)
    {
        _shellyService = shellyService;
    }

    public async Task<SetPowerSwitchResponse> Handle(SetTvPowerSwitchCommand request, CancellationToken cancellationToken)
    {
        var switchResult = await _shellyService.SetTvPowerSwitch(request.IsOn);

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
            var overviewResult = await _shellyService.GetTvPowerSwitchOverview();
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