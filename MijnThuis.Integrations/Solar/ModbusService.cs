using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace MijnThuis.Integrations.Solar;

public interface IModbusService
{
    Task<ModbusDataSet> GetBulkOverview();

    Task<SolarOverview> GetOverview();

    Task<BatteryLevel> GetBatteryLevel();

    Task StartChargingBattery(TimeSpan duration, int power);

    Task StopChargingBattery();

    Task<bool> HasExportLimitation();

    Task SetExportLimitation(float powerLimit);

    Task ResetExportLimitation();
}

internal class ModbusService : BaseService, IModbusService
{
    private readonly string _modbusProxyBaseAddress;

    public ModbusService(IConfiguration configuration) : base(configuration)
    {
        _modbusProxyBaseAddress = configuration.GetValue<string>("MODBUS_PROXY_BASE_ADDRESS");
    }

    public async Task<ModbusDataSet> GetBulkOverview()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        var data = await client.GetFromJsonAsync<ModbusDataSet>("bulk");

        return data;
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

    public async Task StartChargingBattery(TimeSpan duration, int power)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        await client.PostAsync($"battery/startCharging?durationInMinutes={(int)duration.TotalMinutes}&power={power}", null);
    }

    public async Task StopChargingBattery()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        await client.PostAsync($"battery/stopCharging", null);
    }

    public async Task<bool> HasExportLimitation()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        return await client.GetFromJsonAsync<bool>("hasExportLimitation");
    }

    public async Task SetExportLimitation(float powerLimit)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        await client.PostAsync($"setExportLimitation?powerLimit={powerLimit}", null);
    }

    public async Task ResetExportLimitation()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        await client.PostAsync($"resetExportLimitation", null);
    }
}

public record ModbusDataSet
{
    public decimal CurrentSolarPower { get; init; }
    public decimal CurrentBatteryPower { get; init; }
    public decimal CurrentGridPower { get; init; }
    public decimal CurrentConsumptionPower { get; init; }
    public decimal BatteryLevel { get; init; }
    public decimal BatteryHealth { get; init; }
    public int BatteryMaxEnergy { get; init; }
    public StorageControlMode StorageControlMode { get; init; }
    public RemoteControlMode RemoteControlMode { get; init; }
    public RemoteControlMode RemoteControlDefaultMode { get; init; }
    public int RemoteControlCommandTimeout { get; init; }
    public decimal RemoteControlChargeLimit { get; init; }
    public bool IsMaxSelfConsumption { get; init; }
    public ExportControlMode ExportControlMode { get; init; }
    public bool HasExportLimitation { get; init; }
    public decimal ExportPowerLimitation { get; init; }
}

public enum StorageControlMode
{
    /// <summary>
    /// Storage control mode is disabled.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Maximize self consumption mode. Requires a SolarEdge Electricity meter on the grid or load connection point.
    /// </summary>
    MaximizeSelfConsumption = 1,

    /// <summary>
    /// Time of use mode with profile programming. Requires a SolarEdge Electricity meter on the grid or load connection point.
    /// </summary>
    TimeOfUse = 2,

    /// <summary>
    /// Backup only mode. Requires a SolarEdge Backup Interface.
    /// </summary>
    BackupOnly = 3,

    /// <summary>
    /// Remote control mode. The battery charge/discharge state is controlled via an external controller.
    /// </summary>
    RemoteControl = 4
}

/// <summary>
/// Storage Charge/Discharge mode. 
/// </summary>
public enum RemoteControlMode
{
    /// <summary>
    /// Off.
    /// </summary>
    Off = 0,

    /// <summary>
    /// Charge excess PV power only.
    /// Only PV excess power not going to AC is used for charging the battery.
    /// Inverter NominalActivePowerLimit (or the inverter rated power whichever is lower) sets how much power the inverter is producing to the AC.
    /// In this mode, the battery cannot be discharged.
    /// If the PV power is lower than NominalActivePowerLimit the AC production will be equal to the PV power.
    /// </summary>
    ChargeExcessPVPowerOnly = 1,

    /// <summary>
    /// Charge from PV first, before producing power to the AC.
    /// The battery charge has higher priority than AC production.
    /// First charge the battery, then produce AC.
    /// If StorageRemoteCtrl_ChargeLimit is lower than PV, excess power goes to AC according to NominalActivePowerLimit.
    /// If NominalActivePowerLimit is reached and battery StorageRemoteCtrl_ChargeLimit is reached, PV power is curtailed.
    /// </summary>
    ChargeFromPVFirstBeforeProducingPowerToTheAC = 2,

    /// <summary>
    /// Charge from PV plus AC according to the max battery power.
    /// Charge from both PV and AC with priority on PV power.
    /// If PV production is lower than StorageRemoteCtrl_ChargeLimit, the battery will be charged from AC up to NominalActivePowerLimit.
    /// In this case AC power is equal to StorageRemoteCtrl_ChargeLimit minus PV power.
    /// If PV power is larger than StorageRemoteCtrl_ChargeLimit the excess PV power will be directed to the AC up to the NominalActivePowerLimit beyond which the PV is curtailed.
    /// </summary>
    ChargeFromPVPlusACAccordingToTheMaxBatteryPower = 3,

    /// <summary>
    /// Maximize export and discharge battery to meet max inverter AC limit.
    /// AC power is maintained to NominalActivePowerLimit, using PV power and/or battery power.
    /// If the PV power is not sufficient, battery power is used to complement AC power up to StorageRemoteCtrl_DischargeLimit.
    /// In this mode, charging excess power will occur if there is more PV than the AC limit.
    /// </summary>
    MaximizeExportAndDischargeBatteryToMeetMaxInverterACLimit = 4,

    /// <summary>
    /// Discharge to meet loads consumption and not allow discharge to grid.
    /// </summary>
    DischargeToMeetLoadsConsumptionAndNotAllowDischargeToGrid = 5,

    /// <summary>
    /// Maximize self-consumption.
    /// </summary>
    MaximizeSelfConsumption = 7,
}

public enum ExportControlMode
{
    /// <summary>
    /// Export limitation is disabled.
    /// 0000000000000000
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Direct export limitation mode.
    /// The meter is connected on the grid connection point and measures Export/Import power.
    /// Export limit control is performed directly by reading the export power (Site Export <= Site limit).
    /// Only single selection is allowed on bits 0-2.
    /// 0000000000000001
    /// </summary>
    DirectExportLimitation = 1,

    /// <summary>
    /// In-direct export limitation mode.
    /// The meter is connected on the load connection point and measures consumption power.
    /// Export limit control is performed indirectly by reading the consumption power and calculating the export power (Site Production - Site Consumption <= Site limit).
    /// Only single selection is allowed on bits 0-2.
    /// 0000000000000010
    /// </summary>
    IndirectExportLimitation = 2,

    /// <summary>
    /// Production limitation mode.
    /// For this mode, either the internal inverters power production measurements or external site production meter can be used.
    /// The meter measures the site production. The maximum site production will be limited to the site limit.
    /// Only single selection is allowed on bits 0-2.
    /// 0000000000000100
    /// </summary>
    ProductionLimitation = 4
}