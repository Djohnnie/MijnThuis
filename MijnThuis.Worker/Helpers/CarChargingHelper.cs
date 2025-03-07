using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
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
    private readonly MijnThuisDbContext _dbContext;
    private readonly ICarService _carService;
    private readonly ISolarService _solarService;

    public CarChargingHelper(
        MijnThuisDbContext dbContext,
        ICarService carService,
        ISolarService solarService)
    {
        _dbContext = dbContext;
        _carService = carService;
        _solarService = solarService;
    }

    public async Task Do(CarChargingHelperState state, CancellationToken cancellationToken)
    {
        // Get the overview information and location from the car.
        var carOverview = await _carService.GetOverview();
        var carLocation = await _carService.GetLocation();

        // Check if the car is parked within the "Thuis" area and the
        // charge port is open (probably connected to the house).
        state.SetCarIsReadyToCharge(carLocation.Location == "Thuis" && carOverview.IsChargePortOpen);

        // If this makes the car ready to charge?
        if (state.CarIsReadyToCharge)
        {
            // Get information about current solar energy.
            var solarOverview = await _solarService.GetOverview();

            // Add the current solar and consumption power
            // to a list for calculating the average.
            state.CollectedSolarPower.Add(solarOverview.CurrentSolarPower);
            state.CollectedConsumedPower.Add(solarOverview.CurrentConsumptionPower);

            // If a number of measurements have been collected, we can calculate an average.
            if (state.CollectedSolarPower.Count >= state.NumberOfSamplesToCollect)
            {
                // Calculate the average solar and consumption power in kW.
                var currentAverageSolarPower = state.CollectedSolarPower.Average();
                var currentAverageConsumedPower = state.CollectedConsumedPower.Average();

                // Calculate the available solar power, by subtracting the current consumption
                // and adding the power used by the car charging (if it is charging).
                var carChargingPower = carOverview.IsCharging ? carOverview.ChargingAmps * 230M / 1000M : 0M;
                var currentAvailableSolarPower = currentAverageSolarPower - currentAverageConsumedPower + carChargingPower;

                // Calculate the maximum current based on the available solar power.
                var maxPossibleCurrent = currentAvailableSolarPower * 1000M / 230M;

                // Normalize the maximum possible current to the maximum charging amps
                maxPossibleCurrent = maxPossibleCurrent > carOverview.MaxChargingAmps ? carOverview.MaxChargingAmps : maxPossibleCurrent;

                // Clear the list for calculating the average to prepare it for the next calculation.
                state.CollectedSolarPower.Clear();
                state.CollectedConsumedPower.Clear();

                // If the home battery is charged over 95% and the car is not charging
                // or the car is charging at a different charging amps level and the
                // available charging amps level is higher or equal to 2A: Start charging
                // the car at the maximum possible current.
                if (solarOverview.BatteryLevel > 95 && (!carOverview.IsCharging || carOverview.ChargingAmps != (int)maxPossibleCurrent) && (int)maxPossibleCurrent >= 2)
                {
                    // Log the energy charged by the car on start.
                    await CalculateCarChargingEnergyOnStart(state, carOverview);

                    // Start charging the car at the maximum possible current.
                    await _carService.StartCharging((int)maxPossibleCurrent);
                    state.StartedCharging((int)maxPossibleCurrent, carOverview);

                    // Provide the car some time to increase the charging amps.
                    await Task.Delay(TimeSpan.FromMinutes(1));

                    return;
                }

                // If the car is already charging and the current home battery level has
                // dropped below 95% or the maximum possible current has dropped below 2A:
                // Stop charging the car.
                if (carOverview.IsCharging && (solarOverview.BatteryLevel < 95 || (int)maxPossibleCurrent < 2))
                {
                    // Log the energy charged by the car on stop.
                    await CalculateCarChargingEnergyOnStop(state, carOverview);

                    // Stop charging the car.
                    await _carService.StopCharging();
                    state.StoppedCharging();

                    // Provide the car some time to stop charging.
                    await Task.Delay(TimeSpan.FromMinutes(1));

                    return;
                }

                state.SetCharging(carOverview);
            }
            else
            {
                state.GatheringData();
            }
        }
    }

    private async Task CalculateCarChargingEnergyOnStart(CarChargingHelperState state, CarOverview carOverview)
    {
        await CalculateCarChargingEnergy(state, carOverview);

        state.LastMeasurementTimestamp = null;
        state.CurrentChargeSession = null;
    }

    private async Task CalculateCarChargingEnergyOnStop(CarChargingHelperState state, CarOverview carOverview)
    {
        if (await CalculateCarChargingEnergy(state, carOverview))
        {
            state.LastMeasurementTimestamp = DateTime.Now;
        }

        if (!carOverview.IsCharging)
        {
            state.CurrentChargeSession = Guid.CreateVersion7();
            state.LastMeasurementTimestamp = DateTime.Now;
        }
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

                _dbContext.Add(new CarChargingEnergyHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.Now,
                    ChargingSessionId = state.CurrentChargeSession.Value,
                    ChargingAmps = carOverview.ChargingAmps,
                    ChargingDuration = timespan,
                    EnergyCharged = totalEnergy
                });
                await _dbContext.SaveChangesAsync();

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
    ChargingChanged,
    ChargingStopped
}