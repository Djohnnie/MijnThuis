using Djohnnie.SolarEdge.ModBus.TCP;
using Djohnnie.SolarEdge.ModBus.TCP.Constants;
using Djohnnie.SolarEdge.ModBus.TCP.Types;
using Microsoft.Extensions.Configuration;
using UInt16 = Djohnnie.SolarEdge.ModBus.TCP.Types.UInt16;

namespace MijnThuis.Integrations.Solar;

public interface IModbusService
{
    Task<SolarOverview> GetOverview();

    Task<BatteryLevel> GetBatteryLevel();

    Task<EnergyProduced> GetEnergy();

    Task<EnergyOverview> GetEnergyToday();

    Task<EnergyOverview> GetEnergyThisMonth();

    Task<StorageData> GetStorageData(StorageDataRange range);

    Task<bool> IsNotMaxSelfConsumption();

    Task StartChargingBattery(TimeSpan duration, int power);

    Task StopChargingBattery();
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

    public Task<SolarOverview> GetOverview()
    {
        throw new NotImplementedException();
    }

    public async Task<BatteryLevel> GetBatteryLevel()
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
    }

    public Task<EnergyProduced> GetEnergy()
    {
        throw new NotImplementedException();
    }

    public Task<EnergyOverview> GetEnergyToday()
    {
        throw new NotImplementedException();
    }

    public Task<EnergyOverview> GetEnergyThisMonth()
    {
        throw new NotImplementedException();
    }

    public Task<StorageData> GetStorageData(StorageDataRange range)
    {
        throw new NotImplementedException();
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
}