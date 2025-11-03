using MediatR;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess.Repositories;

namespace MijnThuis.Application.Solar.Commands;

public class ChargeBatteryCommandHandler : IRequestHandler<ChargeBatteryCommand, ChargeBatteryResponse>
{
    private readonly IFlagRepository _flagRepository;

    public ChargeBatteryCommandHandler(
        IFlagRepository flagRepository)
    {
        _flagRepository = flagRepository;
    }

    public async Task<ChargeBatteryResponse> Handle(ChargeBatteryCommand request, CancellationToken cancellationToken)
    {
        var chargeUntil = DateTime.Now.Add(request.Duration);

        await _flagRepository.SetManualHomeBatteryChargeFlag(true, request.Power, chargeUntil);

        return new ChargeBatteryResponse
        {
            Success = true
        };
    }
}