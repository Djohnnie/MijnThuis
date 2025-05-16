using Djohnnie.SolarEdge.ModBus.TCP;
using Djohnnie.SolarEdge.ModBus.TCP.Constants;
using Djohnnie.SolarEdge.ModBus.TCP.Types;
using Microsoft.Extensions.Configuration;
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
    private readonly string _modbusAddress;
    private readonly int _modbusPort;

    public ModbusService(IConfiguration configuration) : base(configuration)
    {
        _modbusAddress = configuration.GetValue<string>("MODBUS_ADDRESS");
        _modbusPort = configuration.GetValue<int>("MODBUS_PORT");
    }

    public async Task<SolarOverview> GetOverview()
    {
        return await RetryOnFailure(async () =>
        {
            using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
            await modbusClient.Connect();

            var acPower = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_AC_Power);
            var acPowerSF = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_AC_Power_SF);
            var dcPower = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_DC_Power);
            var dcPowerSF = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_DC_Power_SF);
            var gridPower = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.M1_AC_Power);
            var gridPowerSF = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.M1_AC_Power_SF);
            var batteryPower = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_Instantaneous_Power);
            var soe = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Energy);

            modbusClient.Disconnect();

            var currentBatteryPower = Convert.ToDecimal(batteryPower.Value);
            var currentSolarPower = Convert.ToDecimal(dcPower.Value * Math.Pow(10, dcPowerSF.Value)) + currentBatteryPower;
            var currentGridPower = Convert.ToDecimal(gridPower.Value * Math.Pow(10, gridPowerSF.Value));
            var currentConsumptionPower = Convert.ToDecimal(acPower.Value * Math.Pow(10, acPowerSF.Value)) - currentGridPower;

            return new SolarOverview
            {
                CurrentConsumptionPower = currentConsumptionPower,
                CurrentBatteryPower = currentBatteryPower,
                CurrentGridPower = currentGridPower,
                CurrentSolarPower = currentSolarPower,
                BatteryLevel = Convert.ToInt32(soe.Value),
            };
        }, new SolarOverview());
    }

    public async Task<BatteryLevel> GetBatteryLevel()
    {
        return await RetryOnFailure(async () =>
        {
            using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
            await modbusClient.Connect();

            var soe = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Energy);
            var soh = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Health);
            var max = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_Max_Energy);

            modbusClient.Disconnect();

            return new BatteryLevel
            {
                Level = Convert.ToDecimal(soe.Value),
                Health = Convert.ToDecimal(soh.Value),
                MaxEnergy = Convert.ToDecimal(max.Value)
            };
        }, new BatteryLevel());
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
}