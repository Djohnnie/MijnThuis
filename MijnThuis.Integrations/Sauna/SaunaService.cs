using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace MijnThuis.Integrations.Sauna;


public interface ISaunaService
{
    Task<string> GetState();
    Task<int> GetInsideTemperature();
    Task<int> GetOutsideTemperature();
    Task<decimal> GetPowerUsage();
    Task<string?> GetActiveSession();
    Task<bool> StartSauna();
    Task<bool> StartInfrared();
    Task<bool> StopSauna(string sessionId);
}

public class SaunaService : BaseService, ISaunaService
{
    public SaunaService(IConfiguration configuration) : base(configuration)
    {
    }

    public async Task<string> GetState()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse<StateResponse>>("sensors/state");

        if (result.Content.IsSaunaOn)
        {
            return "Sauna";
        }

        if (result.Content.IsInfraredOn)
        {
            return "Infrarood";
        }

        return "Uit";
    }

    public async Task<int> GetInsideTemperature()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse<TemperatureResponse>>("sensors/temperature/sauna");

        return result.Content.Temperature;
    }

    public async Task<int> GetOutsideTemperature()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse<TemperatureResponse>>("sensors/temperature/outside");

        return result.Content.Temperature;
    }

    public async Task<decimal> GetPowerUsage()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse<PowerResponse>>("sensors/power/sauna");

        return result.Content.InfraredPowerUsage + result.Content.SaunaPowerUsage;
    }

    public async Task<string?> GetActiveSession()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetAsync("sauna/sessions/active");

        if (result.IsSuccessStatusCode)
        {
            var session = await result.Content.ReadFromJsonAsync<BaseResponse<ActiveSessionResponse>>();
            return session.Content.SessionId;
        }

        return null;
    }

    public async Task<bool> StartSauna()
    {
        using var client = InitializeHttpClient();

        var response = await client.PostAsJsonAsync("sauna/sessions/quickstart", new { isSauna = true });

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> StartInfrared()
    {
        using var client = InitializeHttpClient();

        var response = await client.PostAsJsonAsync("sauna/sessions/quickstart", new { isInfrared = true });

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> StopSauna(string sessionId)
    {
        using var client = InitializeHttpClient();

        var response = await client.PutAsync($"sauna/sessions/{sessionId}/cancel", null);

        return response.IsSuccessStatusCode;
    }
}

public class BaseService
{
    private readonly string _baseAddress;
    private readonly string _authToken;

    protected BaseService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("SAUNA_API_BASE_ADDRESS");
        _authToken = configuration.GetValue<string>("SAUNA_API_AUTH_TOKEN");
    }

    protected HttpClient InitializeHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseAddress);
        client.DefaultRequestHeaders.Add("ClientId", _authToken);

        return client;
    }
}

public class BaseResponse<T>
{
    public T Content { get; set; }
}

public class StateResponse
{
    public bool IsSaunaOn { get; set; }
    public bool IsInfraredOn { get; set; }
}

public class TemperatureResponse
{
    public int Temperature { get; set; }
}

public class PowerResponse
{
    public decimal SaunaPowerUsage { get; set; }
    public decimal InfraredPowerUsage { get; set; }
}

public class ActiveSessionResponse
{
    public string SessionId { get; set; }
}