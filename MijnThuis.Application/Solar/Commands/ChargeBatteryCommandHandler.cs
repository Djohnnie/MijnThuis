using MediatR;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Solar.Commands;

public class ChargeBatteryCommandHandler : IRequestHandler<ChargeBatteryCommand, ChargeBatteryResponse>
{
    private readonly IModbusService _modbusService;

    public ChargeBatteryCommandHandler(
        IModbusService modbusService)
    {
        _modbusService = modbusService;
    }

    public async Task<ChargeBatteryResponse> Handle(ChargeBatteryCommand request, CancellationToken cancellationToken)
    {
        await _modbusService.StartChargingBattery(request.Duration, request.Power);

        return new ChargeBatteryResponse
        {
            Success = true
        };
    }
}