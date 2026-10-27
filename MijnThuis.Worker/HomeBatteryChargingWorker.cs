using MijnThuis.Worker.Helpers;
using System.Diagnostics;

namespace MijnThuis.Worker;

public class HomeBatteryChargingWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<HomeBatteryChargingWorker> _logger;

    public HomeBatteryChargingWorker(
        IConfiguration configuration,
        IServiceScopeFactory serviceProvider,
        ILogger<HomeBatteryChargingWorker> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var iterationInMinutes = _configuration.GetValue<int>("ITERATION_IN_MINUTES");

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            using var serviceScope = _serviceProvider.CreateScope();
            var helper = serviceScope.ServiceProvider.GetRequiredService<IHomeBatteryChargingHelper>();

            await RunStep(() => helper.CheckForBatteryCharging(stoppingToken), "battery charging check", stoppingToken);
            await RunStep(() => helper.PrepareCheapestPeriods(stoppingToken), "cheapest periods preparation", stoppingToken);
            await RunStep(() => helper.UpdateChargingSchedule(stoppingToken), "charging schedule update", stoppingToken);

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // Wait for a maximum of 1 minute before the next iteration.
            var duration = TimeSpan.FromMinutes(iterationInMinutes) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }

    private async Task RunStep(Func<Task> step, string stepName, CancellationToken stoppingToken)
    {
        try
        {
            await step();
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Home battery {StepName} failed: {Message}", stepName, ex.Message);
        }
    }
}