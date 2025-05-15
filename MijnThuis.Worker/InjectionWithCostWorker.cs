using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Solar;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class InjectionWithCostWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<SolarHistoryWorker> _logger;

    public InjectionWithCostWorker(
        IServiceScopeFactory serviceProvider,
        ILogger<SolarHistoryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // While the service has not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var repository = serviceScope.ServiceProvider.GetRequiredService<IDayAheadEnergyPricesRepository>();
                var modbusService = serviceScope.ServiceProvider.GetRequiredService<IModbusService>();

                var energyPrice = await repository.GetEnergyPriceForTimestamp(DateTime.Now);
                var solarOverview = await modbusService.GetOverview();
                var hasExportLimitation = await modbusService.HasExportLimitation();

                if (energyPrice.InjectionCentsPerKWh < 0 && solarOverview.BatteryLevel > 95 && !hasExportLimitation)
                {
                    _logger.LogInformation($"Stop exporting energy: Injection price is negative and battery is almost full: {energyPrice.InjectionCentsPerKWh}");
                    await modbusService.SetExportLimitation(0);
                }
                else if (energyPrice.InjectionCentsPerKWh >= 0 || solarOverview.BatteryLevel <= 95 && hasExportLimitation)
                {
                    _logger.LogInformation($"Start exporting energy: Injection price is positive or battery is not yet full: {energyPrice.InjectionCentsPerKWh}");
                    await modbusService.ResetExportLimitation();
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            var stopTimer = Stopwatch.GetTimestamp();

            var duration = TimeSpan.FromMinutes(1) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }
}