using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Power;

public interface IShellyService
{
    Task<PowerSwitchOverview> GetTvPowerSwitchOverview();
    Task<PowerSwitchOverview> GetBureauPowerSwitchOverview();
    Task<bool> SetTvPowerSwitch(bool isOn);
    Task<bool> SetBureauPowerSwitch(bool isOn);
}

public class ShellyService : IShellyService
{
    private readonly string _tvPowerSwitchAddress;
    private readonly string _bureauPowerSwitchAddress;

    public ShellyService(IConfiguration configuration)
    {
        _tvPowerSwitchAddress = configuration.GetValue<string>("TV_POWER_SWITCH_BASE_ADDRESS");
        _bureauPowerSwitchAddress = configuration.GetValue<string>("BUREAU_POWER_SWITCH_BASE_ADDRESS");
    }

    public async Task<PowerSwitchOverview> GetTvPowerSwitchOverview()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<StatusResponse>($"{_tvPowerSwitchAddress}/status");

        return new PowerSwitchOverview
        {
            IsOn = result.Relays.Single().IsOn,
            Power = result.Meters.Single().Power
        };
    }

    public async Task<PowerSwitchOverview> GetBureauPowerSwitchOverview()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<StatusResponse>($"{_bureauPowerSwitchAddress}/status");

        return new PowerSwitchOverview
        {
            IsOn = result.Relays.Single().IsOn,
            Power = result.Meters.Single().Power
        };
    }

    public async Task<bool> SetTvPowerSwitch(bool isOn)
    {
        using var client = InitializeHttpClient();
        var result = await client.GetAsync($"{_tvPowerSwitchAddress}/relay/0?turn={(isOn ? "on" : "off")}");

        return result.IsSuccessStatusCode;
    }

    public async Task<bool> SetBureauPowerSwitch(bool isOn)
    {
        using var client = InitializeHttpClient();
        var result = await client.GetAsync($"{_bureauPowerSwitchAddress}/relay/0?turn={(isOn ? "on" : "off")}");

        return result.IsSuccessStatusCode;
    }

    private HttpClient InitializeHttpClient()
    {
        return new HttpClient();
    }
}

public class StatusResponse
{
    [JsonPropertyName("relays")]
    public List<Relay> Relays { get; set; }

    [JsonPropertyName("meters")]
    public List<Meter> Meters { get; set; }
}

public class Relay
{
    [JsonPropertyName("ison")]
    public bool IsOn { get; set; }
}

public class Meter
{
    [JsonPropertyName("power")]
    public decimal Power { get; set; }
}