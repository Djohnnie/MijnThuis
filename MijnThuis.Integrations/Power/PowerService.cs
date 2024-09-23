using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Power;

public interface IPowerService
{
    Task<PowerOverview> GetOverview();
}

public class PowerService : BaseService, IPowerService
{
    public PowerService(IConfiguration configuration) : base(configuration)
    {

    }

    public async Task<PowerOverview> GetOverview()
    {
        using var client = InitializeHttpClient();
        var result = await client.GetFromJsonAsync<BaseResponse>("api/v1/data");

        return new PowerOverview
        {
            CurrentPower = (int)result.CurrentPower,
            PowerPeak = (int)result.PowerPeak
        };
    }
}

public class BaseService
{
    private readonly string _baseAddress;

    protected BaseService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("POWER_API_BASE_ADDRESS");
    }

    protected HttpClient InitializeHttpClient()
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseAddress);

        return client;
    }
}

public class BaseResponse
{
    [JsonPropertyName("active_power_w")]
    public decimal CurrentPower { get; set; }

    [JsonPropertyName("montly_power_peak_w")]
    public decimal PowerPeak { get; set; }
}