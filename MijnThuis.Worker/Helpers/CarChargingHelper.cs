using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Car;
using MijnThuis.Integrations.Solar;

namespace MijnThuis.Worker.Helpers;

// 1A  - 230V = 230W
// 2A  - 230V = 460W
// 3A  - 230V = 690W
// 4A  - 230V = 920W
// 5A  - 230V = 1150W
// 6A  - 230V = 1380W
// 7A  - 230V = 1610W
// 8A  - 230V = 1840W
// 9A  - 230V = 2070W
// 10A - 230V = 2300W
// 11A - 230V = 2530W
// 12A - 230V = 2760W
// 13A - 230V = 2990W
// 14A - 230V = 3220W
// 15A - 230V = 3450W
// 16A - 230V = 3680W
public interface ICarChargingHelper
{
    Task Do(CarChargingHelperState state, CancellationToken cancellationToken);
}

public class CarChargingHelper : ICarChargingHelper
{
    private readonly IFlagRepository _flagRepository;
    private readonly ICarChargingEnergyHistoryRepository _carChargingEnergyHistoryRepository;
    private readonly ICarService _carService;
    private readonly IModbusService _modbusService;

    public CarChargingHelper(
        IFlagRepository flagRepository,
        ICarChargingEnergyHistoryRepository carChargingEnergyHistoryRepository,
        ICarService carService,
        IModbusService modbusService)
    {
        _flagRepository = flagRepository;
        _carChargingEnergyHistoryRepository = carChargingEnergyHistoryRepository;
        _carService = carService;
        _modbusService = modbusService;
    }

    public async Task Do(CarChargingHelperState state, CancellationToken cancellationToken)
    {
        // Get the overview information and location from the car.
        var carOverview = await _carService.GetOverview();
        var carLocation = await _carService.GetLocation();

        // Gets the manual car charge flag
        var manualCarChargeFlag = await _flagRepository.GetManualCarChargeFlag();

        // Check if the car is parked within the "Thuis" area and the
        // charge port is open (probably connected to the house).
        state.SetCarIsReadyToCharge(carLocation.Location == "Thuis" && carOverview.IsChargePortOpen && carOverview.BatteryLevel < carOverview.ChargeLimit);

        // If this makes the car ready to charge?
        if (state.CarIsReadyToCharge)
        {
            // Get information about current solar energy.
            var solarOverview = await _modbusService.GetOverview();

            // If the car should charge manually and the battery level is above 0%.
            if (manualCarChargeFlag.ShouldCharge && solarOverview.BatteryLevel > 0)
            {
                // Calculate the maximum current to charge the car.
                var ampsToCharge = Math.Min(manualCarChargeFlag.ChargeAmps, solarOverview.BatteryLevel);

                // If the car is not charging, or the car is charging at a different current.
                if (!carOverview.IsCharging || carOverview.ChargingAmps != ampsToCharge)
                {
                    // Log the energy charged by the car on start.
                    await CalculateCarChargingEnergyOnStart(state, carOverview);

                    // Start charging the car at the maximum possible current.
                    await _carService.StartCharging(ampsToCharge);
                    state.SetCharging(manualCarChargeFlag);
                }
            }
            else
            {
                // If the car is already charging, stop charging the car.
                if (carOverview.IsCharging)
                {
                    // Log the energy charged by the car on stop.
                    await CalculateCarChargingEnergyOnStop(state, carOverview);

                    // Stop charging the car.
                    await _carService.StopCharging();
                    state.StoppedCharging();
                }
            }
        }
        else
        {
            // If the car is not ready to charge, but still has a charging session logged...
            if (state.CurrentChargeSession.HasValue)
            {
                // Log the energy charged by the car on stop.
                await CalculateCarChargingEnergyOnStop(state, carOverview);
                state.StoppedCharging();
            }

            // If the manual car charging flag is still set...
            if (manualCarChargeFlag.ShouldCharge)
            {
                // Turn off manual car charging.
                await _flagRepository.SetCarChargingFlag(false, 16);
            }
        }
    }

    private async Task CalculateCarChargingEnergyOnStart(CarChargingHelperState state, CarOverview carOverview)
    {
        if (!await CalculateCarChargingEnergy(state, carOverview))
        {
            state.CurrentChargeSession = Guid.CreateVersion7();
        }

        state.LastMeasurementTimestamp = DateTime.Now;
    }

    private async Task CalculateCarChargingEnergyOnStop(CarChargingHelperState state, CarOverview carOverview)
    {
        await CalculateCarChargingEnergy(state, carOverview);

        state.CurrentChargeSession = null;
        state.LastMeasurementTimestamp = null;
    }

    private async Task<bool> CalculateCarChargingEnergy(CarChargingHelperState state, CarOverview carOverview)
    {
        try
        {
            if (state.CurrentChargeSession.HasValue && state.LastMeasurementTimestamp.HasValue)
            {
                var timespan = DateTime.Now - state.LastMeasurementTimestamp.Value;
                var totalPower = carOverview.ChargingAmps * 230M; // Watts
                var totalEnergy = totalPower * (decimal)timespan.TotalHours; // Wh

                await _carChargingEnergyHistoryRepository.Add(new CarChargingEnergyHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.Now,
                    ChargingSessionId = state.CurrentChargeSession.Value,
                    ChargingAmps = carOverview.ChargingAmps,
                    ChargingDuration = timespan,
                    EnergyCharged = totalEnergy
                });

                return true;
            }
        }
        catch
        {
            // Ignore any exceptions.
        }

        return false;
    }
}

public class CarChargingHelperState
{
    public int NumberOfSamplesToCollect { get; set; }
    public bool CarIsReadyToCharge { get; set; }
    public List<decimal> CollectedSolarPower { get; private set; } = new List<decimal>();
    public List<decimal> CollectedConsumedPower { get; private set; } = new List<decimal>();
    public DateTime? LastMeasurementTimestamp { get; set; } = null;
    public Guid? CurrentChargeSession { get; set; } = null;
    public CarChargingHelperResult Result { get; set; } = new();

    internal void SetCarIsReadyToCharge(bool carIsReadyToCharge)
    {
        CarIsReadyToCharge = carIsReadyToCharge;

        if (!carIsReadyToCharge)
        {
            Result.Type = CarChargingHelperResultType.NotReadyForCharging;
        }
    }

    internal void StartedCharging(int maxPossibleCurrent, CarOverview carOverview)
    {
        Result.ChargingAmps = maxPossibleCurrent;
        Result.ChargingAmpsBefore = carOverview.ChargingAmps;
        Result.Type = carOverview.IsCharging ? CarChargingHelperResultType.ChargingChanged : CarChargingHelperResultType.ChargingStarted;
    }

    internal void SetCharging(CarOverview carOverview)
    {
        Result.ChargingAmps = Result.ChargingAmpsBefore = carOverview.ChargingAmps;
        Result.Type = carOverview.IsCharging ? CarChargingHelperResultType.Charging : CarChargingHelperResultType.NotCharging;
    }

    internal void SetCharging(ManualCarChargeFlag flag)
    {
        Result.ChargingAmps = Result.ChargingAmpsBefore = flag.ChargeAmps;
        Result.Type = flag.ShouldCharge ? CarChargingHelperResultType.ChargingManually : CarChargingHelperResultType.NotCharging;
    }

    internal void StoppedCharging()
    {
        Result.Type = CarChargingHelperResultType.ChargingStopped;
    }

    internal void GatheringData()
    {
        Result.Type = CarChargingHelperResultType.GatheringSolarData;
    }
}

public class CarChargingHelperResult
{
    public CarChargingHelperResultType Type { get; set; }
    public int ChargingAmps { get; set; }
    public int ChargingAmpsBefore { get; set; }
}

public enum CarChargingHelperResultType
{
    NotReadyForCharging,
    NotCharging,
    GatheringSolarData,
    ChargingStarted,
    Charging,
    ChargingManually,
    ChargingChanged,
    ChargingStopped
}