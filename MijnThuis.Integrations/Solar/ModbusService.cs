using Djohnnie.SolarEdge.ModBus.TCP;
using Djohnnie.SolarEdge.ModBus.TCP.Constants;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using Int16 = Djohnnie.SolarEdge.ModBus.TCP.Types.Int16;
using UInt16 = Djohnnie.SolarEdge.ModBus.TCP.Types.UInt16;

namespace MijnThuis.Integrations.Solar;

public interface IModbusService
{
    Task<SolarOverview> GetOverview();

    Task<BatteryLevel> GetBatteryLevel();

    Task<bool> IsNotMaxSelfConsumption();

    Task<bool> IsNotChargingInRemoteControlMode();

    Task StartChargingBattery(TimeSpan duration, int power);

    Task StopChargingBattery();

    Task<bool> HasExportLimitation();

    Task SetExportLimitation(float powerLimit);

    Task ResetExportLimitation();
}
internal class ModbusService : BaseService, IModbusService
{
    private readonly string _modbusProxyBaseAddress;
    private readonly string _modbusAddress;
    private readonly int _modbusPort;

    public ModbusService(IConfiguration configuration) : base(configuration)
    {
        _modbusProxyBaseAddress = configuration.GetValue<string>("MODBUS_PROXY_BASE_ADDRESS");
        _modbusAddress = configuration.GetValue<string>("MODBUS_ADDRESS");
        _modbusPort = configuration.GetValue<int>("MODBUS_PORT");
    }

    public async Task<SolarOverview> GetOverview()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        var data = await client.GetFromJsonAsync<ModbusDataSet>("overview");

        return new SolarOverview
        {
            CurrentConsumptionPower = data.CurrentConsumptionPower,
            CurrentBatteryPower = data.CurrentBatteryPower,
            CurrentGridPower = data.CurrentGridPower,
            CurrentSolarPower = data.CurrentSolarPower,
            BatteryLevel = data.BatteryLevel,
        };
    }

    public async Task<BatteryLevel> GetBatteryLevel()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        var data = await client.GetFromJsonAsync<ModbusDataSet>("battery");

        return new BatteryLevel
        {
            Level = data.BatteryLevel,
            Health = data.BatteryHealth,
            MaxEnergy = data.BatteryMaxEnergy
        };
    }

    public async Task<bool> IsNotMaxSelfConsumption()
    {
        return await RetryOnFailure(async () =>
        {
            using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
            await modbusClient.Connect();

            var storageControlMode = await modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Storage_Control_Mode);
            var remoteControlMode = await modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Remote_Control_Command_Mode);

            modbusClient.Disconnect();

            return storageControlMode.Value != 1 && remoteControlMode.Value != 3;
        }, false);
    }

    public async Task<bool> IsNotChargingInRemoteControlMode()
    {
        return await RetryOnFailure(async () =>
        {
            using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
            await modbusClient.Connect();

            var storageControlMode = await modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Storage_Control_Mode);
            var remoteControlMode = await modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Remote_Control_Command_Mode);

            modbusClient.Disconnect();

            return storageControlMode.Value == 4 && remoteControlMode.Value != 3;
        }, false);
    }

    public async Task StartChargingBattery(TimeSpan duration, int power)
    {
        await RetryOnFailure(async () =>
        {
            using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
            await modbusClient.Connect();

            await modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, (ushort)4);
            await modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Command_Timeout, (uint)duration.TotalSeconds);
            await modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Command_Mode, (ushort)3);
            await modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Charge_Limit, (float)power);

            modbusClient.Disconnect();
        });
    }

    public async Task StopChargingBattery()
    {
        await RetryOnFailure(async () =>
        {
            using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
            await modbusClient.Connect();

            await modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, (ushort)1);

            modbusClient.Disconnect();
        });
    }

    public async Task<bool> HasExportLimitation()
    {
        return await RetryOnFailure(async () =>
        {
            using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
            await modbusClient.Connect();

            var exportControlMode = await modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.ExportControlMode);

            modbusClient.Disconnect();

            return exportControlMode.Value == 1;
        }, false);
    }

    public async Task SetExportLimitation(float powerLimit)
    {
        await RetryOnFailure(async () =>
        {
            using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
            await modbusClient.Connect();

            await modbusClient.WriteSingleRegister(SunspecConsts.ExportControlMode, (ushort)1);
            await modbusClient.WriteSingleRegister(SunspecConsts.ExportControlSiteLimit, powerLimit);

            modbusClient.Disconnect();
        });
    }

    public async Task ResetExportLimitation()
    {
        await RetryOnFailure(async () =>
        {
            using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
            await modbusClient.Connect();

            await modbusClient.WriteSingleRegister(SunspecConsts.ExportControlMode, (ushort)0);
            await modbusClient.WriteSingleRegister(SunspecConsts.ExportControlSiteLimit, 0f);

            modbusClient.Disconnect();
        });
    }

    private static async Task<TResult> RetryOnFailure<TResult>(Func<Task<TResult>> action, TResult defaultValue = default, int maxRetries = 5, int delayMilliseconds = 200)
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
                await Task.Delay(Random.Shared.Next(50, delayMilliseconds));
            }
        } while (error && retries <= maxRetries);

        return defaultValue;
    }

    private static async Task RetryOnFailure(Func<Task> action, int maxRetries = 3, int delayMilliseconds = 500)
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
                await Task.Delay(Random.Shared.Next(100, delayMilliseconds));
            }
        } while (error && retries <= maxRetries);
    }

    public record ModbusDataSet
    {
        public decimal CurrentSolarPower { get; init; }
        public decimal CurrentBatteryPower { get; init; }
        public decimal CurrentGridPower { get; init; }
        public decimal CurrentConsumptionPower { get; init; }
        public int BatteryLevel { get; init; }
        public int BatteryHealth { get; init; }
        public int BatteryMaxEnergy { get; init; }
        public int StorageControlMode { get; init; }
        public int RemoteControlMode { get; init; }
        public int RemoteControlCommandTimeout { get; init; }
        public decimal RemoteControlChargeLimit { get; init; }
        public bool IsMaxSelfConsumption { get; init; }
        public bool HasExportLimitation { get; init; }
        public decimal ExportPowerLimitation { get; init; }
    }
}