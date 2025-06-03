using MediatR;
using MijnThuis.Contracts.Solar;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Application.Solar.Queries;

public class GetMinimalSolarOverviewQueryHandler : IRequestHandler<GetMinimalSolarOverviewQuery, GetSolarOverviewResponse>
{
    private readonly IModbusService _modbusService;

    public GetMinimalSolarOverviewQueryHandler(
        IModbusService modbusService)
    {
        _modbusService = modbusService;
    }

    public async Task<GetSolarOverviewResponse> Handle(GetMinimalSolarOverviewQuery request, CancellationToken cancellationToken)
    {
        var solarResult = await _modbusService.GetOverview();
        var batteryResult = await _modbusService.GetBatteryLevel();

        var result = new GetSolarOverviewResponse();
        result.CurrentSolarPower = solarResult.CurrentSolarPower / 1000M;
        result.CurrentBatteryPower = solarResult.CurrentBatteryPower / 1000M;
        result.CurrentGridPower = -solarResult.CurrentGridPower / 1000M;
        result.CurrentConsumptionPower = solarResult.CurrentConsumptionPower / 1000M;
        result.BatteryLevel = (int)Math.Round(batteryResult.Level);
        result.BatteryHealth = (int)Math.Round(batteryResult.Health);
        result.BatteryMaxEnergy = (int)Math.Round(batteryResult.MaxEnergy);

        return result;
    }
}