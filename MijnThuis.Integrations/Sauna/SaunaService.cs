using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace MijnThuis.Integrations.Sauna;


public interface ISaunaService
{
    Task<string> GetState();
    Task<int> GetInsideTemperature();
    Task<int> GetOutsideTemperature();
    Task<int> GetPowerUsage();
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

    public async Task<int> GetPowerUsage()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse<PowerResponse>>("sensors/power");

        return result.Content.PowerUsage;
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
    public int PowerUsage { get; set; }
}