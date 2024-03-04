using MijnThuis.Integrations.Car;
using MijnThuis.Integrations.Solar;
using System.Diagnostics;

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
        using var serviceScope = _serviceProvider.CreateScope();
        var carService = serviceScope.ServiceProvider.GetService<ICarService>();
        var solarService = serviceScope.ServiceProvider.GetService<ISolarService>();

        var currentSolarPower = new List<decimal>();
        var currentAverageSolarPower = 0M;
        var currentAvailableSolarPower = 0M;
        var maxPossibleCurrent = 0M;

        while (!stoppingToken.IsCancellationRequested)
        {
            var startTimer = Stopwatch.GetTimestamp();

            _logger.LogInformation("Checking car charging state...");

            try
            {
                var carOverview = await carService.GetOverview();
                var carLocation = await carService.GetLocation();

                if (carLocation.Location == "Thuis" && carOverview.IsChargePortOpen)
                {
                    _logger.LogInformation("The car is located at 'Thuis' and the charge port is open!");

                    var solarOverview = await solarService.GetOverview();
                    currentSolarPower.Add(solarOverview.CurrentSolarPower);

                    if (currentSolarPower.Count == 10)
                    {
                        currentAverageSolarPower = currentSolarPower.Average();
                        currentAvailableSolarPower = currentAverageSolarPower - solarOverview.CurrentConsumptionPower + (carOverview.IsCharging ? carOverview.ChargingAmps * 230M / 1000M : 0M);
                        maxPossibleCurrent = currentAvailableSolarPower * 1000M / 230M;
                        _logger.LogInformation($"Battery charge level: {solarOverview.BatteryLevel} %");
                        _logger.LogInformation($"Average solar power past minute: {currentAverageSolarPower:F2} kW [{string.Join(';', currentSolarPower.Select(x => $"{x:F2}"))}]");
                        if (carOverview.IsCharging)
                        {
                            _logger.LogInformation($"Car is charging: {carOverview.ChargingAmps}A");
                        }
                        else
                        {
                            _logger.LogInformation("Car is not charging!");
                        }
                        _logger.LogInformation($"Remaining power: Average solar power {currentAverageSolarPower:F2} kW - Current consumption {solarOverview.CurrentConsumptionPower:F2} kW + Car charging power {carOverview.ChargingAmps * 230M / 1000M} kW");
                        _logger.LogInformation($"Remaining power: {currentAvailableSolarPower:F2} kW");
                        _logger.LogInformation($"Maximum current available: {maxPossibleCurrent:F2} A");
                        currentSolarPower.Clear();

                        if (solarOverview.BatteryLevel > 95 && (!carOverview.IsCharging || carOverview.ChargingAmps != (int)maxPossibleCurrent) && (int)maxPossibleCurrent >= 2)
                        {
                            _logger.LogInformation($"Start charging at {(int)maxPossibleCurrent} A");
                            await carService.StartCharging((int)maxPossibleCurrent);
                        }

                        if (carOverview.IsCharging && (solarOverview.BatteryLevel < 95 || (int)maxPossibleCurrent < 2))
                        {
                            _logger.LogInformation("Stop charging!");
                            await carService.StopCharging();
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("The car is not ready for charging!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            var stopTimer = Stopwatch.GetTimestamp();
            var duration = TimeSpan.FromSeconds(10) - TimeSpan.FromTicks(stopTimer - startTimer);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }
}