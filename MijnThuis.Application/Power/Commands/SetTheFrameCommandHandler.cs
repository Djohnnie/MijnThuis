using MediatR;
using MijnThuis.Contracts.Power;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Samsung;

namespace MijnThuis.Application.Power.Commands;

public class SetTheFrameCommandHandler : IRequestHandler<SetTheFrameCommand, SetTheFrameResponse>
{
    private readonly ISamsungService _samsungService;
    private readonly IFlagRepository _flagRepository;

    public SetTheFrameCommandHandler(
        ISamsungService samsungService,
        IFlagRepository flagRepository)
    {
        _samsungService = samsungService;
        _flagRepository = flagRepository;
    }

    public async Task<SetTheFrameResponse> Handle(SetTheFrameCommand request, CancellationToken cancellationToken)
    {
        var flag = await _flagRepository.GetSamsungTheFrameTokenFlag();
        var token = flag.Token;
        var tokenWasEmpty = string.IsNullOrEmpty(token);

        if (request.TurnOn)
        {
            token = await _samsungService.TurnOnTheFrame(token);
        }
        else
        {
            token = await _samsungService.TurnOffTheFrame(token);
        }

        if (tokenWasEmpty)
        {
            await _flagRepository.SetSamsungTheFrameTokenFlag(token, flag.AutoOn, flag.AutoOff);
        }

        return new SetTheFrameResponse();
    }
}