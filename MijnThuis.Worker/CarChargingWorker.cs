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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initialize dependencies and variables.
        using var serviceScope = _serviceProvider.CreateScope();
        var carService = serviceScope.ServiceProvider.GetService<ICarService>();
        var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();

        var currentSolarPower = new List<decimal>();
        var currentAverageSolarPower = 0M;
        var currentAvailableSolarPower = 0M;
        var maxPossibleCurrent = 0M;
        var carIsReadyToCharge = false;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("Checking car charging state...");

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

                    carIsReadyToCharge = true;

                    // Get information about solar energy.
                    var solarOverview = await solarService.GetOverview();

                    // Add the current solar power (in kW) to a list for calculating the average.
                    currentSolarPower.Add(solarOverview.CurrentSolarPower);

                    // If 10 measurements have been collected, we can calculate an average.
                    if (currentSolarPower.Count == 10)
                    {
                        // Calculate the average solar power in kW.
                        currentAverageSolarPower = currentSolarPower.Average();

                        // Calculate the available solar power, by subtracting the current consumption
                        // and adding the power used by the car charging (if it is charging).
                        currentAvailableSolarPower = currentAverageSolarPower - solarOverview.CurrentConsumptionPower + (carOverview.IsCharging ? carOverview.ChargingAmps * 230M / 1000M : 0M);

                        // Calculate the maximum current based on the available solar power.
                        maxPossibleCurrent = currentAvailableSolarPower * 1000M / 230M;

                        logBuilder.AppendLine($"Battery charge level: {solarOverview.BatteryLevel} %");
                        logBuilder.AppendLine($"Average solar power past minute: {currentAverageSolarPower:F2} kW");
                        logBuilder.AppendLine($"    [{string.Join(';', currentSolarPower.Select(x => $"{x:F2}"))}]");

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
                        logBuilder.AppendLine($"    Average solar power {currentAverageSolarPower:F2} kW");
                        logBuilder.AppendLine($"    Current consumption {solarOverview.CurrentConsumptionPower:F2} kW");
                        logBuilder.AppendLine($"    Car charging power {(carOverview.IsCharging ? carOverview.ChargingAmps * 230M / 1000M : 0M)} kW");
                        logBuilder.AppendLine($"Maximum current available: {maxPossibleCurrent:F2} A");
                        logBuilder.AppendLine($"Maximum charging current available: {carOverview.MaxChargingAmps} A");

                        maxPossibleCurrent = maxPossibleCurrent > carOverview.MaxChargingAmps ? carOverview.MaxChargingAmps - (maxPossibleCurrent - carOverview.MaxChargingAmps < 2 ? 1 : 0) : maxPossibleCurrent - 1;

                        logBuilder.AppendLine($"Maximum current available (recalculated): {(int)maxPossibleCurrent} A");
                        logBuilder.AppendLine($"Maximum power available (recalculated): {(int)maxPossibleCurrent * 230M / 1000M} kW");
                        logBuilder.AppendLine();

                        // Clear the list for calculating the average to
                        // prepare it for the next calculation.
                        currentSolarPower.Clear();

                        // If the home battery is charged over 95% and the car is not charging
                        // or the car is charging at a different charging amps level and the
                        // available charging amps level is higher or equal to 2A: Start charging
                        // the car at the maximum possible current.
                        if (solarOverview.BatteryLevel >= 95 && (!carOverview.IsCharging || carOverview.ChargingAmps != (int)maxPossibleCurrent) && (int)maxPossibleCurrent >= 2)
                        {
                            logBuilder.AppendLine("-----------------------------");
                            logBuilder.AppendLine($"Start charging at {(int)maxPossibleCurrent} A");
                            logBuilder.AppendLine("-----------------------------");
                            await carService.StartCharging((int)maxPossibleCurrent);
                        }

                        // If the car is already charging and the current home battery level has
                        // dropped below 95% or the maximum possible current has dropped below 2A:
                        // Stop charging the car.
                        if (carOverview.IsCharging && (solarOverview.BatteryLevel < 95 || (int)maxPossibleCurrent < 2))
                        {
                            logBuilder.AppendLine("-----------------------------");
                            logBuilder.AppendLine("Stop charging!");
                            logBuilder.AppendLine("-----------------------------");
                            await carService.StopCharging();
                        }
                    }
                }
                else
                {
                    logBuilder.AppendLine("The car is not ready for charging!");
                    carIsReadyToCharge = false;
                }

                _logger.LogInformation(logBuilder.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // If the car is ready to charge, wait for a total of 10 seconds before the next
            // iteration. If the car is not ready to charge (because it is not parked near the
            // house or it is not connected), wait for 5 minutes before the next iteration.
            var duration = (carIsReadyToCharge ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(5)) - TimeSpan.FromTicks(stopTimer - startTimer);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }
}