using MijnThuis.DataAccess;
using MijnThuis.Integrations.Car;
using System.Diagnostics;

namespace MijnThuis.Worker;

public class CarStatusWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<CarStatusWorker> _logger;

    public CarStatusWorker(
        IServiceScopeFactory serviceProvider,
        ILogger<CarStatusWorker> logger)
    {
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
                var carService = serviceScope.ServiceProvider.GetRequiredService<ICarService>();
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<MijnThuisDbContext>();

                //await ProcessCharges(carService, dbContext);
                //await ProcessDrives(carService, dbContext);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // Wait for 15 minutes before the next iteration.
            var duration = TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }
}