using MijnThuis.DataAccess.Repositories;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class DayAheadEnergyPricesWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<HomeBatteryChargingWorker> _logger;

    public DayAheadEnergyPricesWorker(
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
        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var repository = serviceScope.ServiceProvider.GetRequiredService<IDayAheadEnergyPricesRepository>();
                await repository.AddForDay(DateTime.Now.Date, [57.67M, 41.91M, 30.33M, 20.57M, 19.24M, 15.58M, 13.62M, 7.83M, 2.93M, 0M, -0.01M, -0.01M, -0.02M, -2M, -5.01M, -1.95M, -0.01M, 8.05M, 69.95M, 87.46M, 95.20M, 110.08M, 103.29M, 85M]);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // Wait for a maximum of 5 minutes before the next iteration.
            var duration = TimeSpan.FromMinutes(1) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }
}