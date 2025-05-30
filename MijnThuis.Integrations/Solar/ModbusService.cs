﻿using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

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

    public ModbusService(IConfiguration configuration) : base(configuration)
    {
        _modbusProxyBaseAddress = configuration.GetValue<string>("MODBUS_PROXY_BASE_ADDRESS");
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
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        return await client.GetFromJsonAsync<bool>("battery/isNotMaxSelfConsumption");
    }

    public async Task<bool> IsNotChargingInRemoteControlMode()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_modbusProxyBaseAddress);

        return await client.GetFromJsonAsync<bool>("battery/isNotChargingInRemoteControlMode");
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