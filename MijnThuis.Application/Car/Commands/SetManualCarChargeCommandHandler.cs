using MediatR;
using MijnThuis.Contracts.Car;
using MijnThuis.DataAccess.Repositories;

namespace MijnThuis.Application.Car.Commands;

public class SetManualCarChargeCommandHandler : IRequestHandler<SetManualCarChargeCommand, SetManualCarChargeResponse>
{
    private readonly IFlagRepository _flagRepository;

    public SetManualCarChargeCommandHandler(IFlagRepository flagRepository)
    {
        _flagRepository = flagRepository;
    }

    public async Task<SetManualCarChargeResponse> Handle(SetManualCarChargeCommand request, CancellationToken cancellationToken)
    {
        await _flagRepository.SetCarChargingFlag(request.IsEnabled, request.ChargeAmps);
        return new SetManualCarChargeResponse();
    }
}