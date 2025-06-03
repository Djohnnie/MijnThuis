using MijnThuis.DataAccess;
using MijnThuis.DataAccess.Entities;
using MijnThuis.Integrations.Car;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class CarHistoryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<CarHistoryWorker> _logger;

    public CarHistoryWorker(
        IServiceScopeFactory serviceProvider,
        ILogger<CarHistoryWorker> logger)
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

                await ProcessCharges(carService, dbContext);
                await ProcessDrives(carService, dbContext);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // Wait for one hour before the next iteration.
            var duration = TimeSpan.FromHours(1) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }

    private async Task ProcessCharges(ICarService carService, MijnThuisDbContext dbContext)
    {
        var result = await carService.GetCharges();

        foreach (var charge in result.Charges.OrderBy(x => x.StartedAt))
        {
            if (!dbContext.CarChargesHistory.Any(x => x.TessieId == charge.Id))
            {
                dbContext.CarChargesHistory.Add(new CarChargesHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    TessieId = charge.Id,
                    StartedAt = charge.StartedAt,
                    EndedAt = charge.EndedAt,
                    Location = charge.Location,
                    LocationFriendlyName = charge.LocationFriendlyName ?? string.Empty,
                    IsSupercharger = charge.IsSupercharger,
                    IsFastCharger = charge.IsFastCharger,
                    Odometer = charge.Odometer,
                    EnergyAdded = charge.EnergyAdded,
                    EnergyUsed = charge.EnergyUsed,
                    RangeAdded = charge.RangeAdded,
                    BatteryLevelStart = charge.BatteryLevelStart,
                    BatteryLevelEnd = charge.BatteryLevelEnd,
                    DistanceSinceLastCharge = charge.DistanceSinceLastCharge
                });

                await dbContext.SaveChangesAsync();
            }
        }
    }

    private async Task ProcessDrives(ICarService carService, MijnThuisDbContext dbContext)
    {
        var result = await carService.GetDrives();

        foreach (var charge in result.Drives.OrderBy(x => x.StartedAt))
        {
            if (!dbContext.CarDrivesHistory.Any(x => x.TessieId == charge.Id))
            {
                dbContext.CarDrivesHistory.Add(new CarDrivesHistoryEntry
                {
                    Id = Guid.NewGuid(),
                    TessieId = charge.Id,
                    StartedAt = charge.StartedAt,
                    EndedAt = charge.EndedAt,
                    StartingLocation = charge.StartingLocation,
                    EndingLocation = charge.EndingLocation,
                    StartingOdometer = charge.StartingOdometer,
                    EndingOdometer = charge.EndingOdometer,
                    StartingBattery = charge.StartingBattery,
                    EndingBattery = charge.EndingBattery,
                    EnergyUsed = charge.EnergyUsed,
                    RangeUsed = charge.RangeUsed,
                    AverageSpeed = charge.AverageSpeed,
                    MaximumSpeed = charge.MaximumSpeed,
                    Distance = charge.Distance,
                    AverageInsideTemperature = charge.AverageInsideTemperature,
                    AverageOutsideTemperature = charge.AverageOutsideTemperature
                });

                await dbContext.SaveChangesAsync();
            }
        }
    }
}