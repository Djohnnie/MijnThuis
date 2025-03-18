using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.SmartLock;


public interface ISmartLockService
{
    Task<SmartLockOverview> GetOverview();

    Task<List<SmartLockLog>> GetActivityLog();
}

public class SmartLockService : BaseService, ISmartLockService
{
    private readonly string _smartLockId;

    public SmartLockService(IConfiguration configuration) : base(configuration)
    {
        _smartLockId = configuration.GetValue<string>("SMARTLOCK_ID");
    }

    public async Task<SmartLockOverview> GetOverview()
    {
        using var client = await InitializeHttpClient();
        var result = await client.GetFromJsonAsync<SmartLockResponse>($"smartlock/{_smartLockId}");

        return new SmartLockOverview
        {
            State = (SmartLockState)result.State.State,
            DoorState = (SmartLockDoorState)result.State.DoorState,
            BatteryCharge = result.State.BatteryCharge
        };
    }

    public async Task<List<SmartLockLog>> GetActivityLog()
    {
        using var client = await InitializeHttpClient();
        var result = await client.GetFromJsonAsync<List<SmartLockLogResponse>>($"smartlock/{_smartLockId}/log?limit=10");

        return result.Select(x => new SmartLockLog
        {
            Timestamp = x.Timestamp,
            Action = (SmartLockAction)x.Action
        }).ToList();
    }
}

public class BaseService
{
    private readonly string _baseAddress;
    private readonly string _authToken;

    protected BaseService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("SMARTLOCK_API_BASE_ADDRESS");
        _authToken = configuration.GetValue<string>("SMARTLOCK_API_AUTH_TOKEN");
    }

    protected async Task<HttpClient> InitializeHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseAddress);
        client.DefaultRequestHeaders.Add("authorization", $"Bearer {_authToken}");

        return client;
    }
}

internal class SmartLockResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("state")]
    public SmartLockStateResponse State { get; set; }
}

internal class SmartLockStateResponse
{
    [JsonPropertyName("state")]
    public int State { get; set; }

    [JsonPropertyName("doorState")]
    public int DoorState { get; set; }

    [JsonPropertyName("batteryCharge")]
    public int BatteryCharge { get; set; }
}

internal class SmartLockLogResponse
{
    [JsonPropertyName("date")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("action")]
    public int Action { get; set; }
}