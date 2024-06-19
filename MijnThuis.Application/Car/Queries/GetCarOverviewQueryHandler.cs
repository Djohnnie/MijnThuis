using Mapster;
using MediatR;
using Microsoft.Extensions.Configuration;
using MijnThuis.Contracts.Car;
using MijnThuis.Integrations.Car;

namespace MijnThuis.Application.Car.Queries;

public class GetCarOverviewQueryHandler : IRequestHandler<GetCarOverviewQuery, GetCarOverviewResponse>
{
    private readonly ICarService _carService;
    private readonly IChargerService _chargerService;
    private readonly IConfiguration _configuration;

    public GetCarOverviewQueryHandler(
        ICarService carService,
        IChargerService chargerService,
        IConfiguration configuration)
    {
        _carService = carService;
        _chargerService = chargerService;
        _configuration = configuration;
    }

    public async Task<GetCarOverviewResponse> Handle(GetCarOverviewQuery request, CancellationToken cancellationToken)
    {
        var charger1Id = _configuration.GetValue<string>("CHARGER_1_ID");
        var charger2Id = _configuration.GetValue<string>("CHARGER_2_ID");

        var overviewResult = await _carService.GetOverview();
        var batteryResult = await _carService.GetBatteryHealth();
        var locationResult = await _carService.GetLocation();
        //var charger1Result = await _chargerService.GetChargerOverview(charger1Id);
        //var charger2Result = await _chargerService.GetChargerOverview(charger2Id);

        var result = overviewResult.Adapt<GetCarOverviewResponse>();
        result.BatteryHealth = (int)Math.Round(batteryResult.Percentage);
        result.Address = string.Join(", ", locationResult.Address.Split(", ").Take(2));
        if (overviewResult.IsCharging)
        {
            result.ChargingCurrent = $"{overviewResult.ChargingAmps}/{overviewResult.MaxChargingAmps} A";
            result.ChargingRange = $"{overviewResult.ChargeEnergyAdded:F1} kWh ({overviewResult.ChargeRangeAdded:F0} km)";
        }
        result.Charger1 = "?/?"; //$"{charger1Result.NumberOfChargersAvailable} / {charger1Result.NumberOfChargers}";
        result.Charger1Available = false; //charger1Result.NumberOfChargersAvailable > 0;
        result.Charger2 = "?/?"; //$"{charger2Result.NumberOfChargersAvailable} / {charger2Result.NumberOfChargers}";
        result.Charger2Available = false; //charger2Result.NumberOfChargersAvailable > 0;

        return result;
    }
}