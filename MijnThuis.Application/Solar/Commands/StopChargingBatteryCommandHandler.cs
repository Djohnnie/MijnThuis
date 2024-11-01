using MediatR;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Solar.Commands;

public class StopChargingBatteryCommandHandler : IRequestHandler<StopChargingBatteryCommand, StopChargingBatteryResponse>
{
    private readonly IModbusService _modbusService;

    public StopChargingBatteryCommandHandler(
        IModbusService modbusService)
    {
        _modbusService = modbusService;
    }

    public async Task<StopChargingBatteryResponse> Handle(StopChargingBatteryCommand request, CancellationToken cancellationToken)
    {
        await _modbusService.StopChargingBattery();

        return new StopChargingBatteryResponse
        {
            Success = true
        };
    }
}