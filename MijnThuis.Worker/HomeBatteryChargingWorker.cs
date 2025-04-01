using MijnThuis.Worker.Helpers;
using System.Diagnostics;

namespace MijnThuis.Worker;

public class HomeBatteryChargingWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HomeBatteryChargingWorker> _logger;

    public HomeBatteryChargingWorker(
        IConfiguration configuration, IServiceProvider serviceProvider,
        ILogger<HomeBatteryChargingWorker> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var iterationInMinutes = _configuration.GetValue<int>("ITERATION_IN_MINUTES");

        // Keep a flag to know if the battery was charged today.
        DateTime? charged = null;

        // Keep a flag to know when the battery should be charged.
        DateTime? chargeFrom = null;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var helper = serviceScope.ServiceProvider.GetRequiredService<IHomeBatteryChargingHelper>();
                var battery = new BatteryCharged(charged, chargeFrom);
                (charged, chargeFrom) = await helper.Verify(battery, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // Wait for a maximum of 5 minutes before the next iteration.
            var duration = TimeSpan.FromMinutes(iterationInMinutes) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }
}