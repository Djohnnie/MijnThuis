using Djohnnie.SolarEdge.ModBus.TCP;
using Djohnnie.SolarEdge.ModBus.TCP.Constants;
using Djohnnie.SolarEdge.ModBus.TCP.Types;
using Microsoft.Extensions.Caching.Memory;
using MijnThuis.ModbusProxy.Api.Models;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Int16 = Djohnnie.SolarEdge.ModBus.TCP.Types.Int16;
using UInt16 = Djohnnie.SolarEdge.ModBus.TCP.Types.UInt16;

namespace MijnThuis.ModbusProxy.Api.Helpers;

public interface IModbusHelper
{
    Task<ModbusDataSet> GetBulkDataSet();

    Task<ModbusDataSet> GetOverview();

    Task<ModbusDataSet> GetBatteryLevel();

    Task StartChargingBattery(TimeSpan duration, int power);

    Task StopChargingBattery();

    Task<bool> HasExportLimitation();

    Task SetExportLimitation(float powerLimit);

    Task ResetExportLimitation();
}

public class ModbusHelper : IModbusHelper
{
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ModbusHelper> _logger;

    private ModbusClient _modbusClient;

    private bool IsConnected => _modbusClient?.IsConnected ?? false;

    public ModbusHelper(
        IConfiguration configuration,
        IMemoryCache memoryCache,
        ILogger<ModbusHelper> logger)
    {
        _configuration = configuration;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    private async Task Connect()
    {
        if (!IsConnected)
        {
            var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
            var port = _configuration.GetValue<int>("MODBUS_PORT");

            _modbusClient = new ModbusClient(address, port);
            await _modbusClient.ConnectAsync();
        }
    }

    public async Task<ModbusDataSet> GetBulkDataSet()
    {
        return await GetCachedValue("MODBUS_BULK_DATA", async () =>
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                return await RetryOnFailure(async () =>
                {
                    var startTimeStamp = Stopwatch.GetTimestamp();

                    await Connect();

                    var acPower = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.I_AC_Power);
                    var acPowerSF = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.I_AC_Power_SF);
                    var dcPower = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.I_DC_Power);
                    var dcPowerSF = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.I_DC_Power_SF);
                    var gridPower = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.M1_AC_Power);
                    var gridPowerSF = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.M1_AC_Power_SF);
                    var batteryPower = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Battery_1_Instantaneous_Power);
                    var soe = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Battery_1_State_of_Energy);
                    var soh = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Battery_1_State_of_Health);
                    var max = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Battery_1_Max_Energy);
                    var storageControlMode = await _modbusClient.ReadHoldingRegistersAsync<UInt16>(SunspecConsts.Storage_Control_Mode);
                    var remoteControlMode = await _modbusClient.ReadHoldingRegistersAsync<UInt16>(SunspecConsts.Remote_Control_Command_Mode);
                    var remoteControlDefaultMode = await _modbusClient.ReadHoldingRegistersAsync<UInt16>(SunspecConsts.Storage_Charge_Discharge_Default_Mode);
                    var remoteControlCommandTimeout = await _modbusClient.ReadHoldingRegistersAsync<UInt16>(SunspecConsts.Remote_Control_Command_Timeout);
                    var remoteControlChargeLimit = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Remote_Control_Charge_Limit);
                    var exportControlMode = await _modbusClient.ReadHoldingRegistersAsync<UInt16>(SunspecConsts.ExportControlMode);
                    var exportLimitation = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.ExportControlSiteLimit);

                    var currentBatteryPower = Convert.ToDecimal(batteryPower?.Value ?? 0f);
                    var currentSolarPower = Convert.ToDecimal((dcPower?.Value ?? 0) * Math.Pow(10, dcPowerSF?.Value ?? 0)) + currentBatteryPower;
                    var currentGridPower = Convert.ToDecimal((gridPower?.Value ?? 0) * Math.Pow(10, gridPowerSF?.Value ?? 0));
                    var currentConsumptionPower = Convert.ToDecimal((acPower?.Value ?? 0) * Math.Pow(10, acPowerSF?.Value ?? 0)) - currentGridPower;

                    var stopTimeStamp = Stopwatch.GetTimestamp();
                    _logger.LogInformation("Modbus bulk data retrieved in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));

                    var exportControlModeValue = exportControlMode?.Value ?? 0;

                    return new ModbusDataSet
                    {
                        CurrentConsumptionPower = Math.Max(0, currentConsumptionPower),
                        CurrentBatteryPower = currentBatteryPower,
                        CurrentGridPower = currentGridPower,
                        CurrentSolarPower = Math.Max(0, currentSolarPower),
                        BatteryLevel = (decimal)(soe?.Value ?? 0f),
                        BatteryHealth = (decimal)(soh?.Value ?? 0f),
                        BatteryMaxEnergy = Convert.ToInt32(max?.Value ?? 0f),
                        StorageControlMode = (StorageControlMode)(storageControlMode?.Value ?? 0),
                        RemoteControlMode = (RemoteControlMode)(remoteControlMode?.Value ?? 0),
                        RemoteControlDefaultMode = (RemoteControlMode)(remoteControlDefaultMode?.Value ?? 0),
                        RemoteControlCommandTimeout = remoteControlCommandTimeout?.Value ?? 0,
                        RemoteControlChargeLimit = Convert.ToDecimal(remoteControlChargeLimit?.Value ?? 0f),
                        ExportControlMode = (ExportControlMode)exportControlModeValue,
                        HasExportLimitation = exportControlModeValue > 0,
                        ExportPowerLimitation = exportControlModeValue > 0 ? Convert.ToDecimal(exportLimitation?.Value ?? 0f) : 0M,
                    };

                }, maxRetries: 3, delayMilliseconds: 500);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }, absoluteExpirationInSeconds: 15);
    }

    public async Task<ModbusDataSet> GetOverview()
    {
        return await GetCachedValue("MODBUS_OVERVIEW", async () =>
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                return await RetryOnFailure(async () =>
                {
                    var startTimeStamp = Stopwatch.GetTimestamp();

                    await Connect();

                    var acPower = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.I_AC_Power);
                    var acPowerSF = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.I_AC_Power_SF);
                    var dcPower = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.I_DC_Power);
                    var dcPowerSF = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.I_DC_Power_SF);
                    var gridPower = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.M1_AC_Power);
                    var gridPowerSF = await _modbusClient.ReadHoldingRegistersAsync<Int16>(SunspecConsts.M1_AC_Power_SF);
                    var batteryPower = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Battery_1_Instantaneous_Power);
                    var soe = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Battery_1_State_of_Energy);

                    var currentBatteryPower = Convert.ToDecimal(batteryPower?.Value ?? 0f);
                    var currentSolarPower = Convert.ToDecimal((dcPower?.Value ?? 0) * Math.Pow(10, dcPowerSF?.Value ?? 0)) + currentBatteryPower;
                    var currentGridPower = Convert.ToDecimal((gridPower?.Value ?? 0) * Math.Pow(10, gridPowerSF?.Value ?? 0));
                    var currentConsumptionPower = Convert.ToDecimal((acPower?.Value ?? 0) * Math.Pow(10, acPowerSF?.Value ?? 0)) - currentGridPower;

                    var stopTimeStamp = Stopwatch.GetTimestamp();
                    _logger.LogInformation("Modbus overview data retrieved in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));

                    return new ModbusDataSet
                    {
                        CurrentConsumptionPower = Math.Max(0, currentConsumptionPower),
                        CurrentBatteryPower = currentBatteryPower,
                        CurrentGridPower = currentGridPower,
                        CurrentSolarPower = Math.Max(0, currentSolarPower),
                        BatteryLevel = (decimal)(soe?.Value ?? 0f)
                    };

                }, maxRetries: 3, delayMilliseconds: 500);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }, absoluteExpirationInSeconds: 5);
    }

    public async Task<ModbusDataSet> GetBatteryLevel()
    {
        return await GetCachedValue("MODBUS_BATTERY_LEVEL", async () =>
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                return await RetryOnFailure(async () =>
                {
                    var startTimeStamp = Stopwatch.GetTimestamp();

                    await Connect();

                    var soe = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Battery_1_State_of_Energy);
                    var soh = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Battery_1_State_of_Health);
                    var max = await _modbusClient.ReadHoldingRegistersAsync<Float32>(SunspecConsts.Battery_1_Max_Energy);

                    var stopTimeStamp = Stopwatch.GetTimestamp();
                    _logger.LogInformation("Modbus battery level data retrieved in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));

                    return new ModbusDataSet
                    {
                        BatteryLevel = (decimal)(soe?.Value ?? 0f),
                        BatteryHealth = (decimal)(soh?.Value ?? 0f),
                        BatteryMaxEnergy = Convert.ToInt32(max?.Value ?? 0f)
                    };

                }, maxRetries: 3, delayMilliseconds: 500);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }, absoluteExpirationInSeconds: 60);
    }

    public async Task StartChargingBattery(TimeSpan duration, int power)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();

            await RetryOnFailure(async () =>
            {
                await Connect();

                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.Storage_Control_Mode, (ushort)StorageControlMode.RemoteControl);
                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.Remote_Control_Command_Timeout, (uint)duration.TotalSeconds);
                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.Remote_Control_Command_Mode, (ushort)RemoteControlMode.ChargeFromPVPlusACAccordingToTheMaxBatteryPower);
                //await _modbusClient.WriteSingleRegister(SunspecConsts.Storage_Charge_Discharge_Default_Mode, (ushort)RemoteControlMode.MaximizeSelfConsumption);
                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.Remote_Control_Charge_Limit, (float)power);
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task StopChargingBattery()
    {
        try
        {
            await _semaphoreSlim.WaitAsync();

            await RetryOnFailure(async () =>
            {
                await Connect();

                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.Storage_Control_Mode, (ushort)StorageControlMode.MaximizeSelfConsumption);
                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.Remote_Control_Command_Timeout, (uint)0);
                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.Remote_Control_Command_Mode, (ushort)RemoteControlMode.MaximizeSelfConsumption);
                //await _modbusClient.WriteSingleRegister(SunspecConsts.Storage_Charge_Discharge_Default_Mode, (ushort)RemoteControlMode.MaximizeSelfConsumption);
                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.Remote_Control_Charge_Limit, (float)5000);
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<bool> HasExportLimitation()
    {
        try
        {
            await _semaphoreSlim.WaitAsync();

            return await RetryOnFailure(async () =>
            {
                var startTimeStamp = Stopwatch.GetTimestamp();

                await Connect();

                var exportControlMode = await _modbusClient.ReadHoldingRegistersAsync<UInt16>(SunspecConsts.ExportControlMode);

                var stopTimeStamp = Stopwatch.GetTimestamp();

                _logger.LogInformation("Modbus export limitation retrieved in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));

                return exportControlMode?.Value == 1;
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task SetExportLimitation(float powerLimit)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();

            await RetryOnFailure(async () =>
            {
                var startTimeStamp = Stopwatch.GetTimestamp();

                await Connect();

                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.ExportControlMode, (ushort)ExportControlMode.DirectExportLimitation);
                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.ExportControlSiteLimit, powerLimit);

                var stopTimeStamp = Stopwatch.GetTimestamp();

                _logger.LogInformation("Modbus export limitation set in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task ResetExportLimitation()
    {
        try
        {
            await _semaphoreSlim.WaitAsync();

            await RetryOnFailure(async () =>
            {
                var startTimeStamp = Stopwatch.GetTimestamp();

                await Connect();

                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.ExportControlMode, (ushort)ExportControlMode.Disabled);
                await _modbusClient.WriteSingleRegisterAsync(SunspecConsts.ExportControlSiteLimit, 0f);

                var stopTimeStamp = Stopwatch.GetTimestamp();

                _logger.LogInformation("Modbus export limitation reset in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    private async Task<TResult> RetryOnFailure<TResult>(Func<Task<TResult>> action, int maxRetries = 5, int delayMilliseconds = 500, [CallerMemberName] string caller = "")
    {
        var retries = 0;

        while (true)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                retries++;
                _logger.LogError(ex, "MODBUS FAILURE @ {Caller}: {Message}", caller, ex.Message);

                if (retries >= maxRetries)
                {
                    throw;
                }

                await Task.Delay(Random.Shared.Next(200, delayMilliseconds));
            }
        }
    }

    private async Task RetryOnFailure(Func<Task> action, int maxRetries = 3, int delayMilliseconds = 500, [CallerMemberName] string caller = "")
    {
        var retries = 0;

        while (true)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                retries++;
                _logger.LogError(ex, "MODBUS FAILURE @ {Caller}: {Message}", caller, ex.Message);

                if (retries >= maxRetries)
                {
                    throw;
                }

                await Task.Delay(Random.Shared.Next(200, delayMilliseconds));
            }
        }
    }

    private async Task<T> GetCachedValue<T>(string key, Func<Task<T>> valueFactory, int absoluteExpirationInSeconds)
    {
        return await _memoryCache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(absoluteExpirationInSeconds);
            return await valueFactory();
        });
    }
}