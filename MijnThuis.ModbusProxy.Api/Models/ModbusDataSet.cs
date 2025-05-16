namespace MijnThuis.ModbusProxy.Api.Models;

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