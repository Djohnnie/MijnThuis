using MijnThuis.Integrations.Forecast;
using MijnThuis.Integrations.Solar;
using System.Diagnostics;

namespace MijnThuis.Worker;

internal class HomeBatteryChargingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HomeBatteryChargingWorker> _logger;

    public HomeBatteryChargingWorker(
        IServiceProvider serviceProvider,
        ILogger<HomeBatteryChargingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Solar-forecast:
        // https://doc.forecast.solar/api:estimate
        // https://api.forecast.solar/estimate/:lat/:lon/:dec/:az/:kwp
        // https://api.forecast.solar/estimate/51.06/4.36/39/43/5
        //
        // 6 panels SW: 2.5kWp (223° of 43°, 39°)
        // 3 panelen NE: 1.2kWp (43° of -137°, 38°)
        // 4 panelen SE: 1.6kWp (133° of -47°, 10°)

        const decimal LATITUDE = 51.06M;
        const decimal LONGITUDE = 4.36M;
        const int DELAY_IN_HOURS = 1;

        DateTime? charged = null;

        // While the service is not requested to stop...
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Use a timestamp to calculate the duration of the whole process.
                var startTimer = Stopwatch.GetTimestamp();

                using var serviceScope = _serviceProvider.CreateScope();
                var forecastService = serviceScope.ServiceProvider.GetService<IForecastService>();
                var modbusService = serviceScope.ServiceProvider.GetService<IModbusService>();

                if (charged.HasValue && charged.Value.Date < DateTime.Today.Date)
                {
                    charged = null;
                    _logger.LogInformation("A new day and last charge was yesterday. 'Charged' has been reset!");
                }

                if (charged == null && DateTime.Now > DateTime.Today.AddHours(DELAY_IN_HOURS))
                {
                    var batteryLevel = await modbusService.GetBatteryLevel();

                    if (batteryLevel.Level < 20)
                    {
                        _logger.LogInformation($"Battery level is below 20%: Start charging at {DateTime.Now}!");
                        var zw6 = await forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, 43M, 2.5M);
                        var no3 = await forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 39M, -137M, 1.2M);
                        var zo4 = await forecastService.GetSolarForecastEstimate(LATITUDE, LONGITUDE, 10M, -47M, 1.6M);
                        var totalWattHoursEstimate = zw6.EstimatedWattHoursToday + no3.EstimatedWattHoursToday + zo4.EstimatedWattHoursToday;
                        _logger.LogInformation($"{zw6.EstimatedWattHoursToday}Wh + {no3.EstimatedWattHoursToday}Wh + {zo4.EstimatedWattHoursToday}Wh = {totalWattHoursEstimate}Wh");

                        var wattHoursToCharge = 10000 - totalWattHoursEstimate;
                        var durationInSeconds = wattHoursToCharge * 3.6M;
                        _logger.LogInformation($"Battery should charge {wattHoursToCharge}Wh!");

                        if (wattHoursToCharge > 0)
                        {
                            durationInSeconds = durationInSeconds > 18000 ? 18000 : durationInSeconds;

                            // Charge the battery with the remaining watt hours.
                            await modbusService.StartChargingBattery(TimeSpan.FromSeconds((double)durationInSeconds), 1000);
                        }
                    }

                    charged = DateTime.Now;
                }


                // Calculate the duration for this whole process.
                var stopTimer = Stopwatch.GetTimestamp();

                // Wait for a maximum of 5 minutes before the next iteration.
                var duration = TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds((stopTimer - startTimer) / (double)Stopwatch.Frequency);

                if (duration > TimeSpan.Zero)
                {
                    await Task.Delay(duration, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Something went wrong: {ex.Message}");
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }


        // (20/10/2024) 2354 + 832 + 1384 = 4570

        //Console.WriteLine(result.EstimatedWattHoursToday);

        //using var modbusClient = new ModbusClient("x.x.x.x", 0);

        //await modbusClient.Connect();
        //var storageControlMode = await modbusClient.ReadHoldingRegisters<Djohnnie.SolarEdge.ModBus.TCP.Types.UInt16>(SunspecConsts.Storage_Control_Mode);
        //if (storageControlMode.Value != 1)
        //{
        //    await modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, 1);
        //}

        //DateTime? startDateTime = null;
        //bool isCharging = false;

        //float chargeStarted = 0;
        //float energyAdded = 0;
        //float chargeAdded = 0;

        //// While the service is not requested to stop...
        //while (!stoppingToken.IsCancellationRequested)
        //{
        //    try
        //    {
        //        storageControlMode = await modbusClient.ReadHoldingRegisters<Djohnnie.SolarEdge.ModBus.TCP.Types.UInt16>(SunspecConsts.Storage_Control_Mode);
        //        var storagePower = await modbusClient.ReadHoldingRegisters<Djohnnie.SolarEdge.ModBus.TCP.Types.Float32>(SunspecConsts.Battery_1_Instantaneous_Power);
        //        var storageState = await modbusClient.ReadHoldingRegisters<Djohnnie.SolarEdge.ModBus.TCP.Types.Float32>(SunspecConsts.Battery_1_State_of_Energy);



        //        // Charge from grid during 5 minutes and pause for 10 minutes.
        //        if (!startDateTime.HasValue && !isCharging)
        //        {
        //            Console.WriteLine($"Start charging at {DateTime.Now}");
        //            await modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, 2);
        //            startDateTime = DateTime.Now;
        //            isCharging = true;
        //            chargeStarted = storageState.Value;
        //        }

        //        if (startDateTime.HasValue && isCharging)
        //        {
        //            if (DateTime.Now - startDateTime.Value > TimeSpan.FromMinutes(5))
        //            {
        //                Console.WriteLine($"STOP charging at {DateTime.Now}");
        //                await modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, 1);
        //                isCharging = false;
        //                chargeAdded = storageState.Value - chargeStarted;
        //                energyAdded = storagePower.Value / 60f * 5f;
        //                chargeStarted = 0;
        //                Console.WriteLine($"{energyAdded}Wh added, {chargeAdded}% added");
        //            }
        //        }

        //        if (startDateTime.HasValue && !isCharging)
        //        {
        //            if (DateTime.Now - startDateTime.Value > TimeSpan.FromMinutes(15))
        //            {
        //                Console.WriteLine($"Start charging at {DateTime.Now}");
        //                await modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, 2);
        //                startDateTime = DateTime.Now;
        //                isCharging = true;
        //                chargeStarted = storageState.Value;
        //            }
        //        }




        //        Console.WriteLine($"{storagePower}W ({storageState}%, {storageControlMode})");

        //        await Task.Delay(10000);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, ex.Message);
        //    }
        //}
    }
}