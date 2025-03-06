using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.Integrations.Car;
using MijnThuis.Integrations.Solar;
using System.Diagnostics;
using System.Text;

namespace MijnThuis.Worker;

public class CarChargingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CarChargingWorker> _logger;

    public CarChargingWorker(
        IServiceProvider serviceProvider,
        ILogger<CarChargingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int numberOfSamplesToCollect = 6;
        var collectedSolarPower = new List<decimal>();
        var collectedConsumedPower = new List<decimal>();
        var currentAverageSolarPower = 0M;
        var currentAverageConsumedPower = 0M;
        var currentAvailableSolarPower = 0M;
        var maxPossibleCurrent = 0M;
        var carIsReadyToCharge = false;

        DateTime? lastMeasurementTimestamp = null;
        Guid? currentChargeSession = null;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Initialize dependencies and variables.
            using var serviceScope = _serviceProvider.CreateScope();
            using var dbContext = serviceScope.ServiceProvider.GetService<MijnThuisDbContext>();
            var carService = serviceScope.ServiceProvider.GetService<ICarService>();
            var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();

            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("Checking solar power, consumption and car charging state...");

            try
            {
                // Get the information about the car.
                var carOverview = await carService.GetOverview();

                // Get the location of the car.
                var carLocation = await carService.GetLocation();

                // If the car is parked within the "Thuis" area and the charge port is open (probably connected to the house).
                if (carLocation.Location == "Thuis" && carOverview.IsChargePortOpen)
                {
                    logBuilder.AppendLine("The car is located at 'Thuis' and the charge port is open!");
                    logBuilder.AppendLine($"Collecting data for calculating average... ({collectedSolarPower.Count + 1}/{numberOfSamplesToCollect})");
                    logBuilder.AppendLine();

                    carIsReadyToCharge = true;

                    // Get information about solar energy.
                    var solarOverview = await solarService.GetOverview();

                    // Add the current solar power (in kW) to a list for calculating the average.
                    collectedSolarPower.Add(solarOverview.CurrentSolarPower);
                    collectedConsumedPower.Add(solarOverview.CurrentConsumptionPower);

                    // If 20 measurements have been collected, we can calculate an average.
                    if (collectedSolarPower.Count >= numberOfSamplesToCollect)
                    {
                        // Calculate the average solar power in kW.
                        currentAverageSolarPower = collectedSolarPower.Average();
                        currentAverageConsumedPower = collectedConsumedPower.Average();

                        // Calculate the available solar power, by subtracting the current consumption
                        // and adding the power used by the car charging (if it is charging).
                        currentAvailableSolarPower = currentAverageSolarPower - currentAverageConsumedPower + (carOverview.IsCharging ? carOverview.ChargingAmps * 230M / 1000M : 0M);

                        // Calculate the maximum current based on the available solar power.
                        maxPossibleCurrent = currentAvailableSolarPower * 1000M / 230M;

                        logBuilder.AppendLine($"Battery charge level: {solarOverview.BatteryLevel} %");
                        logBuilder.AppendLine($"Average solar power from collected samples: {currentAverageSolarPower:F2} kW");
                        logBuilder.AppendLine($"    [{string.Join(", ", collectedSolarPower.Select(x => $"{x:F2}"))}]");
                        logBuilder.AppendLine($"Average consumed power from collected samples: {currentAverageConsumedPower:F2} kW");
                        logBuilder.AppendLine($"    [{string.Join(", ", collectedConsumedPower.Select(x => $"{x:F2}"))}]");

                        // If the car is already charging, log the current charging amps.
                        if (carOverview.IsCharging)
                        {
                            logBuilder.AppendLine($"Car is charging: {carOverview.ChargingAmps}A");
                        }
                        else
                        {
                            logBuilder.AppendLine("Car is not charging!");
                        }

                        logBuilder.AppendLine($"Remaining power: {currentAvailableSolarPower:F2} kW");
                        logBuilder.AppendLine($"  = Average solar power {currentAverageSolarPower:F2} kW");
                        logBuilder.AppendLine($"  - Average consumption {currentAverageConsumedPower:F2} kW");
                        logBuilder.AppendLine($"  + Car charging power {(carOverview.IsCharging ? carOverview.ChargingAmps * 230M / 1000M : 0M)} kW");
                        logBuilder.AppendLine($"Maximum current available: {maxPossibleCurrent:F2} A");

                        maxPossibleCurrent = maxPossibleCurrent > carOverview.MaxChargingAmps ? carOverview.MaxChargingAmps : maxPossibleCurrent;

                        logBuilder.AppendLine($"Maximum current available (recalculated): {(int)maxPossibleCurrent}/{carOverview.MaxChargingAmps} A");
                        logBuilder.AppendLine($"Maximum power available (recalculated): {(int)maxPossibleCurrent * 230M / 1000M} kW");
                        logBuilder.AppendLine();

                        // Clear the list for calculating the average to
                        // prepare it for the next calculation.
                        collectedSolarPower.Clear();
                        collectedConsumedPower.Clear();

                        // If the home battery is charged over 95% and the car is not charging
                        // or the car is charging at a different charging amps level and the
                        // available charging amps level is higher or equal to 2A: Start charging
                        // the car at the maximum possible current.
                        if (solarOverview.BatteryLevel > 95 && (!carOverview.IsCharging || carOverview.ChargingAmps != (int)maxPossibleCurrent) && (int)maxPossibleCurrent >= 2)
                        {
                            logBuilder.AppendLine("-----------------------------");
                            logBuilder.AppendLine($"Start charging at {(int)maxPossibleCurrent} A");
                            logBuilder.AppendLine("-----------------------------");
                            
                            if (await CalculateCarChargingEnergy(dbContext, currentChargeSession, lastMeasurementTimestamp, carOverview))
                            {
                                lastMeasurementTimestamp = DateTime.Now;
                            }

                            if (!carOverview.IsCharging)
                            {
                                currentChargeSession = Guid.CreateVersion7();
                                lastMeasurementTimestamp = DateTime.Now;
                            }

                            await carService.StartCharging((int)maxPossibleCurrent);

                            await Task.Delay(TimeSpan.FromMinutes(1));
                        }

                        // If the car is already charging and the current home battery level has
                        // dropped below 95% or the maximum possible current has dropped below 2A:
                        // Stop charging the car.
                        if (carOverview.IsCharging && (solarOverview.BatteryLevel <= 95 || (int)maxPossibleCurrent < 2))
                        {
                            logBuilder.AppendLine("-----------------------------");
                            logBuilder.AppendLine("Stop charging!");
                            logBuilder.AppendLine("-----------------------------");
                            await CalculateCarChargingEnergy(dbContext, currentChargeSession, lastMeasurementTimestamp, carOverview);
                            await carService.StopCharging();
                            lastMeasurementTimestamp = null;
                            currentChargeSession = null;
                            await Task.Delay(TimeSpan.FromMinutes(1));
                        }
                    }
                }
                else
                {
                    logBuilder.AppendLine("The car is not ready for charging!");
                    carIsReadyToCharge = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            _logger.LogInformation(logBuilder.ToString().TrimEnd());

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // If the car is ready to charge, wait for a total of 10 seconds before the next
            // iteration. If the car is not ready to charge (because it is not parked near the
            // house or it is not connected), wait for 5 minutes before the next iteration.
            var duration = (carIsReadyToCharge ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(5)) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }

    private async Task<bool> CalculateCarChargingEnergy(MijnThuisDbContext dbContext, Guid? currentChargeSession, DateTime? lastMeasurementTimestamp, CarOverview carOverview)
    {
        try
        {
            if (currentChargeSession.HasValue && lastMeasurementTimestamp.HasValue)
            {
                var timespan = DateTime.Now - lastMeasurementTimestamp.Value;
                var totalPower = carOverview.ChargingAmps * 230M; // Watts
                var totalEnergy = totalPower * (decimal)timespan.TotalHours; // Wh

                dbContext.Add(new CarChargingEnergyHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.Now,
                    ChargingSessionId = currentChargeSession.Value,
                    ChargingAmps = carOverview.ChargingAmps,
                    ChargingDuration = timespan,
                    EnergyCharged = totalEnergy
                });
                await dbContext.SaveChangesAsync();

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