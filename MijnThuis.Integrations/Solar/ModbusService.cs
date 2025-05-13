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
        var error = false;

        do
        {
            try
            {
                using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
                await modbusClient.Connect();

                var power = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_AC_Power);
                var powerSF = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.I_AC_Power_SF);
                var exportPower = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.M1_AC_Power);
                var exportPowerSF = await modbusClient.ReadHoldingRegisters<Int16>(SunspecConsts.M1_AC_Power_SF);
                var batteryPower = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_Instantaneous_Power);
                var soe = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Energy);

                modbusClient.Disconnect();

                error = false;

                var currentConsumptionPower = Convert.ToDecimal(power.Value * Math.Pow(10, powerSF.Value));
                var currentBatteryPower = Convert.ToDecimal(batteryPower.Value);
                var currentGridPower = Convert.ToDecimal(exportPower.Value * Math.Pow(10, exportPowerSF.Value));

                return new SolarOverview
                {
                    CurrentConsumptionPower = currentConsumptionPower,
                    CurrentBatteryPower = currentBatteryPower,
                    CurrentGridPower = currentGridPower,
                    CurrentSolarPower = currentConsumptionPower + currentBatteryPower + currentGridPower,
                    BatteryLevel = Convert.ToInt32(soe.Value),
                };
            }
            catch
            {
                error = true;
                await Task.Delay(500);
            }
        } while (error);

        return new SolarOverview();
    }

    public async Task<BatteryLevel> GetBatteryLevel()
    {
        var error = false;

        do
        {
            try
            {
                using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
                await modbusClient.Connect();

                var soe = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Energy);
                var soh = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_State_of_Health);
                var max = await modbusClient.ReadHoldingRegisters<Float32>(SunspecConsts.Battery_1_Max_Energy);

                modbusClient.Disconnect();

                error = false;

                return new BatteryLevel
                {
                    Level = Convert.ToDecimal(soe.Value),
                    Health = Convert.ToDecimal(soh.Value),
                    MaxEnergy = Convert.ToDecimal(max.Value)
                };
            }
            catch
            {
                error = true;
                await Task.Delay(500);
            }
        } while (error);

        return new BatteryLevel();
    }

    public async Task<bool> IsNotMaxSelfConsumption()
    {
        using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
        await modbusClient.Connect();

        var storageControlMode = await modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Storage_Control_Mode);
        var remoteControlMode = await modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Remote_Control_Command_Mode);

        modbusClient.Disconnect();

        return storageControlMode.Value != 1 && remoteControlMode.Value != 3;
    }

    public async Task<bool> IsNotChargingInRemoteControlMode()
    {
        using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
        await modbusClient.Connect();

        var storageControlMode = await modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Storage_Control_Mode);
        var remoteControlMode = await modbusClient.ReadHoldingRegisters<UInt16>(SunspecConsts.Remote_Control_Command_Mode);

        modbusClient.Disconnect();

        return storageControlMode.Value == 4 && remoteControlMode.Value != 3;
    }

    public async Task StartChargingBattery(TimeSpan duration, int power)
    {
        using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
        await modbusClient.Connect();

        await modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, (ushort)4);
        await modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Command_Timeout, (uint)duration.TotalSeconds);
        await modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Command_Mode, (ushort)3);
        await modbusClient.WriteSingleRegister(SunspecConsts.Remote_Control_Charge_Limit, (float)power);

        modbusClient.Disconnect();
    }

    public async Task StopChargingBattery()
    {
        using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
        await modbusClient.Connect();

        await modbusClient.WriteSingleRegister(SunspecConsts.Storage_Control_Mode, (ushort)1);

        modbusClient.Disconnect();
    }

    public async Task SetExportLimitation(float powerLimit)
    {
        using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
        await modbusClient.Connect();

        await modbusClient.WriteSingleRegister(SunspecConsts.ExportControlMode, (ushort)0);
        await modbusClient.WriteSingleRegister(SunspecConsts.ExportControlSiteLimit, powerLimit);

        modbusClient.Disconnect();
    }

    public async Task ResetExportLimitation()
    {
        using var modbusClient = new ModbusClient(_modbusAddress, _modbusPort);
        await modbusClient.Connect();

        await modbusClient.WriteSingleRegister(SunspecConsts.ExportControlMode, (ushort)0);
        await modbusClient.WriteSingleRegister(SunspecConsts.ExportControlSiteLimit, 0f);

        modbusClient.Disconnect();
    }
}