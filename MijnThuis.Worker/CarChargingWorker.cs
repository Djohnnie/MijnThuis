using MijnThuis.Worker.Helpers;
using System.Diagnostics;

namespace MijnThuis.Worker;

public class CarChargingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<CarChargingWorker> _logger;

    public CarChargingWorker(
        IServiceScopeFactory serviceProvider,
        ILogger<CarChargingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var state = new CarChargingHelperState
        {
            NumberOfSamplesToCollect = 6,
        };

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var helper = serviceScope.ServiceProvider.GetRequiredService<ICarChargingHelper>();
                await helper.Do(state, stoppingToken);
                LogResults(state);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // If the car is ready to charge, wait for a total of 10 seconds before the next
            // iteration. If the car is not ready to charge (because it is not parked near the
            // house or it is not connected), wait for 5 minutes before the next iteration.
            var duration = (state.CarIsReadyToCharge ? TimeSpan.FromMinutes(1) : TimeSpan.FromMinutes(5)) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }

    private void LogResults(CarChargingHelperState state)
    {
        var message = state.Result.Type switch
        {
            CarChargingHelperResultType.NotReadyForCharging => "Car is not ready for charging.",
            CarChargingHelperResultType.NotCharging => "The car is not charging, but is ready.",
            CarChargingHelperResultType.GatheringSolarData => "Gathering solar data...",
            CarChargingHelperResultType.ChargingStarted => $"Car has started charging at {state.Result.ChargingAmps}A.",
            CarChargingHelperResultType.Charging => $"Car is charging at {state.Result.ChargingAmps}A.",
            CarChargingHelperResultType.ChargingManually => $"Car is manually charging at {state.Result.ChargingAmps}A.",
            CarChargingHelperResultType.ChargingChanged => $"Car has changed charging from {state.Result.ChargingAmpsBefore}A to {state.Result.ChargingAmps}A.",
            CarChargingHelperResultType.ChargingStopped => "Car has stopped charging.",
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogInformation(message);
        }
    }
}