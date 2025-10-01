using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using MijnThuis.DataAccess.Entities;
using MijnThuis.DataAccess.Repositories;
using MijnThuis.Integrations.Power;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class DayAheadEnergyPricesWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceProvider;
    private readonly ILogger<DayAheadEnergyPricesWorker> _logger;

    public DayAheadEnergyPricesWorker(
        IConfiguration configuration,
        IServiceScopeFactory serviceProvider,
        ILogger<DayAheadEnergyPricesWorker> logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var startHistoryFrom = _configuration.GetValue<DateTime>("SOLAR_HISTORY_START");

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            // Use a timestamp to calculate the duration of the whole process.
            var startTimer = Stopwatch.GetTimestamp();

            try
            {
                using var serviceScope = _serviceProvider.CreateScope();
                var energyPricesService = serviceScope.ServiceProvider.GetRequiredService<IEnergyPricesService>();
                var flagRepository = serviceScope.ServiceProvider.GetRequiredService<IFlagRepository>();
                var energyPricesRepository = serviceScope.ServiceProvider.GetRequiredService<IDayAheadEnergyPricesRepository>();

                var consumptionTariffExpressionFlag = await flagRepository.GetConsumptionTariffExpressionFlag();
                var consumptionTariffScript = CSharpScript.Create<decimal>(consumptionTariffExpressionFlag.Expression, globalsType: typeof(PriceGlobals));

                var injectionTariffExpressionFlag = await flagRepository.GetInjectionTariffExpressionFlag();
                var injectionTariffScript = CSharpScript.Create<decimal>(injectionTariffExpressionFlag.Expression, globalsType: typeof(PriceGlobals));

                var tomorrow = DateTime.Today.AddDays(1);
                var previousEntry = await energyPricesRepository.GetLatestEnergyPrices();

                if (previousEntry.Count > 0 && previousEntry.Last().From.Date == DateTime.Today.AddDays(+1))
                {
                    _logger.LogInformation("'Day Ahead' energy prices are up to date.");
                }
                else
                {

                    if (previousEntry.Count > 0)
                    {
                        startHistoryFrom = previousEntry.Last().From.Date.AddDays(1);
                    }

                    _logger.LogInformation($"'Day Ahead' energy prices are up to date. should update from {startHistoryFrom} until {tomorrow}.");

                    var dateToProcess = startHistoryFrom;

                    while (dateToProcess <= tomorrow)
                    {
                        _logger.LogInformation($"Processing 'Day Ahead' energy prices for {dateToProcess}...");

                        try
                        {
                            var energyPrices = await energyPricesService.GetEnergyPricesForDate(dateToProcess);

                            foreach (var price in energyPrices.Prices)
                            {
                                var minutesPerPeriod = dateToProcess >= new DateTime(2025, 10, 1) ? 15 : 60;

                                await energyPricesRepository.AddEnergyPrice(new DayAheadEnergyPricesEntry
                                {
                                    Id = Guid.NewGuid(),
                                    From = price.TimeStamp,
                                    To = price.TimeStamp.AddMinutes(minutesPerPeriod).AddSeconds(-1),
                                    EuroPerMWh = price.Price,
                                    ConsumptionTariffFormulaExpression = dateToProcess < new DateTime(2025, 5, 1) ? "" : consumptionTariffExpressionFlag.Expression,
                                    ConsumptionCentsPerKWh = dateToProcess < new DateTime(2025, 5, 1) ? price.Price / 10M : await RunExpression(consumptionTariffScript, price.Price / 10M),
                                    InjectionTariffFormulaExpression = dateToProcess < new DateTime(2025, 5, 1) ? "" : injectionTariffExpressionFlag.Expression,
                                    InjectionCentsPerKWh = dateToProcess < new DateTime(2025, 5, 1) ? price.Price / 10M : await RunExpression(injectionTariffScript, price.Price / 10M)
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            if (dateToProcess == tomorrow)
                            {
                                _logger.LogInformation($"No data found yet for 'Day Ahead' energy prices tomorrow {dateToProcess}");
                            }
                            else
                            {
                                _logger.LogError(ex, $"Error fetching 'Day Ahead' energy prices for {dateToProcess}: {ex.Message}");
                            }

                            break;
                        }

                        dateToProcess = dateToProcess.AddDays(1);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                _logger.LogError(ex, ex.Message);
            }

            // Calculate the duration for this whole process.
            var stopTimer = Stopwatch.GetTimestamp();

            // Wait for a maximum of 1 hour before the next iteration.
            var duration = TimeSpan.FromHours(1) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

            if (duration > TimeSpan.Zero)
            {
                await Task.Delay(duration, stoppingToken);
            }
        }
    }

    private async Task<decimal> RunExpression(Script<decimal> script, decimal price)
    {
        var delegateToRun = script.CreateDelegate();
        return await delegateToRun.Invoke(globals: new PriceGlobals { price = price });
    }
}

public class PriceGlobals
{
    public decimal price { get; set; }
}