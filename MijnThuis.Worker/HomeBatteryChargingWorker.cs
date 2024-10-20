using Djohnnie.SolarEdge.ModBus.TCP;
using Djohnnie.SolarEdge.ModBus.TCP.Constants;
using MijnThuis.Integrations.Forecast;

namespace MijnThuis.Worker;

internal class HomeBatteryChargingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CarChargingWorker> _logger;

    public HomeBatteryChargingWorker(
        IServiceProvider serviceProvider,
        ILogger<CarChargingWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Zonne-energie voorspelling:
        // https://doc.forecast.solar/api:estimate
        // https://api.forecast.solar/estimate/:lat/:lon/:dec/:az/:kwp
        // https://api.forecast.solar/estimate/51.06/4.36/39/43/5
        //
        // 6 panelen ZW: 2.5kWp (223° of 43°, 39°)
        // 3 panelen NO: 1.2kWp (43° of -137°, 38°)
        // 4 panelen ZO: 1.6kWp (133° of -47°, 10°)

        using var serviceScope = _serviceProvider.CreateScope();
        var service = serviceScope.ServiceProvider.GetService<IForecastService>();

        var zw6 = await service.GetSolarForecastEstimate(51.06M, 4.36M, 39M, 43M, 2.5M);
        var no3 = await service.GetSolarForecastEstimate(51.06M, 4.36M, 39M, -137M, 1.2M);
        var zo4 = await service.GetSolarForecastEstimate(51.06M, 4.36M, 10M, -47M, 1.6M);
        var total = zw6.EstimatedWattHoursToday + no3.EstimatedWattHoursToday + zo4.EstimatedWattHoursToday;
        Console.WriteLine($"{zw6.EstimatedWattHoursToday} + {no3.EstimatedWattHoursToday} + {zo4.EstimatedWattHoursToday} = {total}");

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