using MediatR;
using MijnThuis.Contracts.Power;
using MijnThuis.Integrations.Samsung;

namespace MijnThuis.Application.Power.Commands;

public class SetTheFrameCommandHandler : IRequestHandler<SetTheFrameCommand, SetTheFrameResponse>
{
    private readonly ISamsungService _samsungService;

    public SetTheFrameCommandHandler(ISamsungService samsungService)
    {
        _samsungService = samsungService;
    }

    public async Task<SetTheFrameResponse> Handle(SetTheFrameCommand request, CancellationToken cancellationToken)
    {
        if (request.TurnOn)
        {
            await _samsungService.TurnOnTheFrame();
        }
        else
        {
            await _samsungService.TurnOffTheFrame();
        }

        return new SetTheFrameResponse();
    }
}