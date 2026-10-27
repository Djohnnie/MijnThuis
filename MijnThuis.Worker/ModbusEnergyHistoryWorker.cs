using Microsoft.EntityFrameworkCore;
using MijnThuis.DataAccess;
using MijnThuis.Integrations.Solar;
using MijnThuis.Worker.Helpers;

namespace MijnThuis.Worker;

// Builds the quarter-hour solar energy history from Modbus power samples, filling in any
// periods the SolarEdge API did not provide so the home battery charging schedule keeps
// working without that API.
internal class ModbusEnergyHistoryWorker : BackgroundService
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<ModbusEnergyHistoryWorker> _logger;

    public ModbusEnergyHistoryWorker(
        IServiceScopeFactory serviceProvider,
        ILogger<ModbusEnergyHistoryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var accumulator = new ModbusEnergyAccumulator();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var modbusService = serviceScope.ServiceProvider.GetRequiredService<IModbusService>();

                var data = await modbusService.GetBulkOverview();
                var completedSlots = accumulator.AddSample(DateTime.Now, data);

                if (completedSlots.Count > 0)
                {
                    using var dbContext = serviceScope.ServiceProvider.GetRequiredService<MijnThuisDbContext>();

                    foreach (var slot in completedSlots)
                    {
                        if (await dbContext.SolarEnergyHistory.AnyAsync(x => x.Date == slot.Date, stoppingToken))
                        {
                            continue;
                        }

                        dbContext.SolarEnergyHistory.Add(slot);
                        _logger.LogInformation("Stored Modbus energy history for {Slot}: {Consumption}Wh consumed, {Production}Wh produced", slot.Date, slot.Consumption, slot.Production);
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect Modbus energy history");
            }

            await Task.Delay(SampleInterval, stoppingToken);
        }
    }
}
