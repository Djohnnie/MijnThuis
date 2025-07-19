using MediatR;
using Microsoft.Extensions.Configuration;
using MijnThuis.Contracts.Car;
using MijnThuis.DataAccess.Repositories;

namespace MijnThuis.Application.Car.Commands;

public class SetManualCarChargeCommandHandler : IRequestHandler<SetManualCarChargeCommand, SetManualCarChargeResponse>
{
    private readonly IFlagRepository _flagRepository;
    private readonly IConfiguration _configuration;

    public SetManualCarChargeCommandHandler(
        IFlagRepository flagRepository,
        IConfiguration configuration)
    {
        _flagRepository = flagRepository;
        _configuration = configuration;
    }

    public async Task<SetManualCarChargeResponse> Handle(SetManualCarChargeCommand request, CancellationToken cancellationToken)
    {
        var pin = _configuration.GetValue<string>("PINCODE");

        if (request.IsEnabled && request.Pin != pin)
        {
            return new SetManualCarChargeResponse
            {
                Success = false
            };
        }

        await _flagRepository.SetCarChargingFlag(request.IsEnabled, request.ChargeAmps);
        return new SetManualCarChargeResponse { Success = true };
    }
}