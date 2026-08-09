using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Solar;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class InjectionWithCostWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<InjectionWithCostWorker> _logger;
    private readonly TimeSpan _minimumSwitchInterval = TimeSpan.FromMinutes(10);

    private const decimal BatteryNotChargingThresholdWatts = 100M;
    private const decimal BatteryChargingThresholdWatts = 250M;
    private const int RequiredConsecutiveDecisions = 2;

    private DateTimeOffset _lastSwitchAt = DateTimeOffset.MinValue;
    private int _limitDecisionCount;
    private int _resetDecisionCount;

    public InjectionWithCostWorker(
        IServiceScopeFactory serviceProvider,
        ILogger<InjectionWithCostWorker> logger)
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

                var shouldLimitExport =
                    energyPrice.InjectionCentsPerKWh < 0 &&
                    solarOverview.CurrentBatteryPower <= BatteryNotChargingThresholdWatts;
                var shouldResetExportLimit =
                    energyPrice.InjectionCentsPerKWh >= 0 ||
                    solarOverview.CurrentBatteryPower > BatteryChargingThresholdWatts;

                if (!hasExportLimitation && shouldLimitExport)
                {
                    _limitDecisionCount++;
                    _resetDecisionCount = 0;

                    var canSwitch = DateTimeOffset.Now - _lastSwitchAt >= _minimumSwitchInterval;

                    if (_limitDecisionCount >= RequiredConsecutiveDecisions && canSwitch)
                    {
                        _logger.LogInformation(
                            "Stop exporting energy: Injection price is negative and battery is not charging enough. Price={InjectionPrice}, BatteryPower={BatteryPower}",
                            energyPrice.InjectionCentsPerKWh,
                            solarOverview.CurrentBatteryPower);

                        await modbusService.SetExportLimitation(0);
                        _lastSwitchAt = DateTimeOffset.Now;
                        _limitDecisionCount = 0;
                    }
                }
                else if (hasExportLimitation && shouldResetExportLimit)
                {
                    _resetDecisionCount++;
                    _limitDecisionCount = 0;

                    var canSwitch = DateTimeOffset.Now - _lastSwitchAt >= _minimumSwitchInterval;

                    if (_resetDecisionCount >= RequiredConsecutiveDecisions && canSwitch)
                    {
                        _logger.LogInformation(
                            "Start exporting energy: Injection price is positive or battery is charging. Price={InjectionPrice}, BatteryPower={BatteryPower}",
                            energyPrice.InjectionCentsPerKWh,
                            solarOverview.CurrentBatteryPower);

                        await modbusService.ResetExportLimitation();
                        _lastSwitchAt = DateTimeOffset.Now;
                        _resetDecisionCount = 0;
                    }
                }
                else
                {
                    _limitDecisionCount = 0;
                    _resetDecisionCount = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            var stopTimer = Stopwatch.GetTimestamp();

            var duration = TimeSpan.FromMinutes(2) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }
}