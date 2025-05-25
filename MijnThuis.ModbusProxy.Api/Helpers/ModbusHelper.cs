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

    Task<bool> IsNotMaxSelfConsumption();

    Task<bool> IsNotChargingInRemoteControlMode();

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

    private bool IsConnected => _modbusClient == null ? false : _modbusClient.IsConnected;

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
            await _modbusClient.Connect();
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

                    var acPower = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_AC_Power);
                    var acPowerSF = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_AC_Power_SF);
                    var dcPower = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_DC_Power);
                    var dcPowerSF = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_DC_Power_SF);
                    var gridPower = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.M1_AC_Power);
                    var gridPowerSF = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.M1_AC_Power_SF);
                    var batteryPower = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_Instantaneous_Power);
                    var soe = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Energy);
                    var soh = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Health);
                    var max = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_Max_Energy);
                    var storageControlMode = await _modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Storage_Control_Mode);
                    var remoteControlMode = await _modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Remote_Control_Command_Mode);
                    var remoteControlCommandTimeout = await _modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Remote_Control_Command_Timeout);
                    var remoteControlChargeLimit = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Remote_Control_Charge_Limit);
                    var exportControlMode = await _modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.ExportControlMode);
                    var exportLimitation = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.ExportControlSiteLimit);

                    var currentBatteryPower = Convert.ToDecimal(batteryPower.Value);
                    var currentSolarPower = Convert.ToDecimal(dcPower.Value * Math.Pow(10, dcPowerSF.Value)) + currentBatteryPower;
                    var currentGridPower = Convert.ToDecimal(gridPower.Value * Math.Pow(10, gridPowerSF.Value));
                    var currentConsumptionPower = Convert.ToDecimal(acPower.Value * Math.Pow(10, acPowerSF.Value)) - currentGridPower;

                    var stopTimeStamp = Stopwatch.GetTimestamp();
                    _logger.LogInformation("Modbus bulk data retrieved in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));

                    return new ModbusDataSet
                    {
                        CurrentConsumptionPower = Math.Max(0, currentConsumptionPower),
                        CurrentBatteryPower = currentBatteryPower,
                        CurrentGridPower = currentGridPower,
                        CurrentSolarPower = Math.Max(0, currentSolarPower),
                        BatteryLevel = Convert.ToInt32(soe.Value),
                        BatteryHealth = Convert.ToInt32(soh.Value),
                        BatteryMaxEnergy = Convert.ToInt32(max.Value),
                        StorageControlMode = storageControlMode.Value,
                        RemoteControlMode = remoteControlMode.Value,
                        RemoteControlCommandTimeout = remoteControlCommandTimeout.Value,
                        RemoteControlChargeLimit = Convert.ToDecimal(remoteControlChargeLimit.Value),
                        HasExportLimitation = exportControlMode.Value == 1,
                        ExportPowerLimitation = Convert.ToDecimal(exportLimitation.Value),
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
            var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
            var port = _configuration.GetValue<int>("MODBUS_PORT");

            try
            {
                await _semaphoreSlim.WaitAsync();

                return await RetryOnFailure(async () =>
                {
                    var startTimeStamp = Stopwatch.GetTimestamp();

                    await Connect();

                    var acPower = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_AC_Power);
                    var acPowerSF = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_AC_Power_SF);
                    var dcPower = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_DC_Power);
                    var dcPowerSF = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_DC_Power_SF);
                    var gridPower = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.M1_AC_Power);
                    var gridPowerSF = await _modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.M1_AC_Power_SF);
                    var batteryPower = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_Instantaneous_Power);
                    var soe = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Energy);

                    var currentBatteryPower = Convert.ToDecimal(batteryPower.Value);
                    var currentSolarPower = Convert.ToDecimal(dcPower.Value * Math.Pow(10, dcPowerSF.Value)) + currentBatteryPower;
                    var currentGridPower = Convert.ToDecimal(gridPower.Value * Math.Pow(10, gridPowerSF.Value));
                    var currentConsumptionPower = Convert.ToDecimal(acPower.Value * Math.Pow(10, acPowerSF.Value)) - currentGridPower;

                    var stopTimeStamp = Stopwatch.GetTimestamp();
                    _logger.LogInformation("Modbus overview data retrieved in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));

                    return new ModbusDataSet
                    {
                        CurrentConsumptionPower = Math.Max(0, currentConsumptionPower),
                        CurrentBatteryPower = currentBatteryPower,
                        CurrentGridPower = currentGridPower,
                        CurrentSolarPower = Math.Max(0, currentSolarPower),
                        BatteryLevel = Convert.ToInt32(soe.Value)
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
            var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
            var port = _configuration.GetValue<int>("MODBUS_PORT");

            try
            {
                await _semaphoreSlim.WaitAsync();

                return await RetryOnFailure(async () =>
                {
                    var startTimeStamp = Stopwatch.GetTimestamp();

                    await Connect();

                    var soe = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Energy);
                    var soh = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Health);
                    var max = await _modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_Max_Energy);

                    var stopTimeStamp = Stopwatch.GetTimestamp();
                    _logger.LogInformation("Modbus battery level data retrieved in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));

                    return new ModbusDataSet
                    {
                        BatteryLevel = Convert.ToInt32(soe.Value),
                        BatteryHealth = Convert.ToInt32(soh.Value),
                        BatteryMaxEnergy = Convert.ToInt32(max.Value)
                    };

                }, maxRetries: 3, delayMilliseconds: 500);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }, absoluteExpirationInSeconds: 60);
    }

    public async Task<bool> IsNotMaxSelfConsumption()
    {
        var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
        var port = _configuration.GetValue<int>("MODBUS_PORT");

        try
        {
            await _semaphoreSlim.WaitAsync();

            return await RetryOnFailure(async () =>
            {
                await Connect();

                var storageControlMode = await _modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Storage_Control_Mode);
                var remoteControlMode = await _modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Remote_Control_Command_Mode);

                return storageControlMode.Value != 1 && remoteControlMode.Value != 3;
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<bool> IsNotChargingInRemoteControlMode()
    {
        var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
        var port = _configuration.GetValue<int>("MODBUS_PORT");

        try
        {
            await _semaphoreSlim.WaitAsync();

            return await RetryOnFailure(async () =>
            {
                await Connect();

                var storageControlMode = await _modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Storage_Control_Mode);
                var remoteControlMode = await _modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Remote_Control_Command_Mode);

                return storageControlMode.Value == 4 && remoteControlMode.Value != 3;
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task StartChargingBattery(TimeSpan duration, int power)
    {
        var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
        var port = _configuration.GetValue<int>("MODBUS_PORT");

        try
        {
            await _semaphoreSlim.WaitAsync();

            await RetryOnFailure(async () =>
            {
                await Connect();

                await _modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, (ushort)4);
                await _modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Command_Timeout, (uint)duration.TotalSeconds);
                await _modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Command_Mode, (ushort)3);
                await _modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Charge_Limit, (float)power);
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task StopChargingBattery()
    {
        var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
        var port = _configuration.GetValue<int>("MODBUS_PORT");

        try
        {
            await _semaphoreSlim.WaitAsync();

            await RetryOnFailure(async () =>
            {
                await Connect();

                await _modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, (ushort)1);
                await _modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Charge_Limit, (float)5000);
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task<bool> HasExportLimitation()
    {
        var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
        var port = _configuration.GetValue<int>("MODBUS_PORT");

        try
        {
            await _semaphoreSlim.WaitAsync();

            return await RetryOnFailure(async () =>
            {
                var startTimeStamp = Stopwatch.GetTimestamp();

                await Connect();

                var exportControlMode = await _modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.ExportControlMode);

                var stopTimeStamp = Stopwatch.GetTimestamp();

                _logger.LogInformation("Modbus export limitation retrieved in {ElapsedTime}ms", (stopTimeStamp - startTimeStamp) / (Stopwatch.Frequency / 1000));

                return exportControlMode.Value == 1;
            });
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }

    public async Task SetExportLimitation(float powerLimit)
    {
        var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
        var port = _configuration.GetValue<int>("MODBUS_PORT");

        try
        {
            await _semaphoreSlim.WaitAsync();

            await RetryOnFailure(async () =>
            {
                var startTimeStamp = Stopwatch.GetTimestamp();

                await Connect();

                await _modbusClient.WriteSingleRegister(SunspecConsts.ExportControlMode, (ushort)1);
                await _modbusClient.WriteSingleRegister(SunspecConsts.ExportControlSiteLimit, powerLimit);

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
        var address = _configuration.GetValue<string>("MODBUS_ADDRESS");
        var port = _configuration.GetValue<int>("MODBUS_PORT");

        try
        {
            await _semaphoreSlim.WaitAsync();

            await RetryOnFailure(async () =>
            {
                var startTimeStamp = Stopwatch.GetTimestamp();

                await Connect();

                await _modbusClient.WriteSingleRegister(SunspecConsts.ExportControlMode, (ushort)0);
                await _modbusClient.WriteSingleRegister(SunspecConsts.ExportControlSiteLimit, 0f);

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
        var error = false;
        var retries = 0;

        do
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                error = true;
                retries++;
                _logger.LogError($"MODBUS FAILURE @ {caller}: {ex.Message}");
                _logger.LogError(ex, ex.Message);

                if (retries >= maxRetries)
                {
                    throw;
                }

                await Task.Delay(Random.Shared.Next(200, delayMilliseconds));
            }
        } while (error);

        return default;
    }

    private async Task RetryOnFailure(Func<Task> action, int maxRetries = 3, int delayMilliseconds = 500, [CallerMemberName] string caller = "")
    {
        var error = false;
        var retries = 0;

        do
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                error = true;
                retries++;
                _logger.LogError($"MODBUS FAILURE @ {caller}: {ex.Message}");
                _logger.LogError(ex, ex.Message);

                if (retries >= maxRetries)
                {
                    throw;
                }

                await Task.Delay(Random.Shared.Next(200, delayMilliseconds));
            }
        } while (error && retries <= maxRetries);
    }

    private async Task<T> GetCachedValue<T>(string key, Func<Task<T>> valueFactory, int absoluteExpirationInSeconds)
    {
        if (_memoryCache.TryGetValue(key, out T value))
        {
            return value;
        }

        value = await valueFactory();
        _memoryCache.Set(key, value, TimeSpan.FromSeconds(absoluteExpirationInSeconds));

        return value;
    }
}