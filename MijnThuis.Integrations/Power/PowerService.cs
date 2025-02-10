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
            ActiveTarrif = result.ActiveTarrif,
            TotalImport = result.TotalImport ?? 0M,
            Tarrif1Import = result.Tarrif1Import ?? 0M,
            Tarrif2Import = result.Tarrif2Import ?? 0M,
            TotalExport = result.TotalExport ?? 0M,
            Tarrif1Export = result.Tarrif1Export ?? 0M,
            Tarrif2Export = result.Tarrif2Export ?? 0M,
            TotalGas = result.TotalGas ?? 0M,
            CurrentPower = (int)(result.CurrentPower ?? 0M),
            PowerPeak = (int)(result.PowerPeak ?? 0M)
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
    [JsonPropertyName("active_tariff")]
    public byte ActiveTarrif { get; set; }

    [JsonPropertyName("total_power_import_kwh")]
    public decimal? TotalImport { get; set; }

    [JsonPropertyName("total_power_import_t1_kwh")]
    public decimal? Tarrif1Import { get; set; }

    [JsonPropertyName("total_power_import_t2_kwh")]
    public decimal? Tarrif2Import { get; set; }

    [JsonPropertyName("total_power_export_kwh")]
    public decimal? TotalExport { get; set; }

    [JsonPropertyName("total_power_export_t1_kwh")]
    public decimal? Tarrif1Export { get; set; }

    [JsonPropertyName("total_power_export_t2_kwh")]
    public decimal? Tarrif2Export { get; set; }

    [JsonPropertyName("total_gas_m3")]
    public decimal? TotalGas { get; set; }

    [JsonPropertyName("active_power_w")]
    public decimal? CurrentPower { get; set; }

    [JsonPropertyName("montly_power_peak_w")]
    public decimal? PowerPeak { get; set; }
}