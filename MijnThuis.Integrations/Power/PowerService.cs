using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Power;

public interface IPowerService
{
    Task<PowerOverview> GetOverview();
    //Task<bool> GetTvStatus();
    //Task SetTvStatus(bool status);
    //Task GetBureauStatus();
    //Task SetBureauStatus(bool status);
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
            CurrentPower = result.CurrentPower,
            PowerPeak = result.PowerPeak
        };
    }

    //public async Task<bool> GetTvStatus()
    //{
    //    using var client = InitializeHttpClient();
    //    var result = await client.GetFromJsonAsync<BaseResponse>("api/v1/data");

    //    return new PowerOverview
    //    {
    //        CurrentPower = result.CurrentPower,
    //        PowerPeak = result.PowerPeak
    //    };
    //}

    //public async Task SetTvStatus(bool status)
    //{

    //}

    //public async Task GetBureauStatus()
    //{

    //}

    //public async Task SetBureauStatus(bool status)
    //{

    //}
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
    public int CurrentPower { get; set; }

    [JsonPropertyName("montly_power_peak_w")]
    public int PowerPeak { get; set; }
}