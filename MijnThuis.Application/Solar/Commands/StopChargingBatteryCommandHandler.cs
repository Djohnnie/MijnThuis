using MediatR;
using MijnThuis.Contracts.Solar;
using MijnThuis.DataAccess.Repositories;

namespace MijnThuis.Application.Solar.Commands;

public class StopChargingBatteryCommandHandler : IRequestHandler<StopChargingBatteryCommand, StopChargingBatteryResponse>
{
    private readonly IFlagRepository _flagRepository;

    public StopChargingBatteryCommandHandler(
        IFlagRepository flagRepository)
    {
        _flagRepository = flagRepository;
    }

    public async Task<StopChargingBatteryResponse> Handle(StopChargingBatteryCommand request, CancellationToken cancellationToken)
    {
        await _flagRepository.SetManualHomeBatteryChargeFlag(false, 0, DateTime.MinValue);

        return new StopChargingBatteryResponse
        {
            Success = true
        };
    }
}