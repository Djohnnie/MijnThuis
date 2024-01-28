using MediatR;
using MijnThuis.Contracts.Power;
using MijnThuis.Integrations.Power;

namespace MijnThuis.Application.Power.Commands;

public class WakeOnLanCommandHandler : IRequestHandler<WakeOnLanCommand, WakeOnLanResponse>
{
    private readonly IWakeOnLanService _wakeOnLanService;

    public WakeOnLanCommandHandler(IWakeOnLanService wakeOnLanService)
    {
        _wakeOnLanService = wakeOnLanService;
    }

    public async Task<WakeOnLanResponse> Handle(WakeOnLanCommand command, CancellationToken cancellationToken)
    {
        await _wakeOnLanService.Wake();

        return new WakeOnLanResponse();
    }
}