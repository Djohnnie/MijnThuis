using IdentityModel;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Heating;


public interface IHeatingService
{
    Task<HeatingOverview> GetOverview();
}

public class HeatingService : BaseService, IHeatingService
{
    private readonly string _authToken;
    private readonly string _siteId;

    public HeatingService(IConfiguration configuration) : base(configuration)
    {
        _authToken = configuration.GetValue<string>("HEATING_API_AUTH_TOKEN");
        _siteId = configuration.GetValue<string>("HEATING_SITE_ID");
    }

    public async Task<HeatingOverview> GetOverview()
    {
        try
        {
            using var client = await InitializeHttpClient();
            var result = await client.GetFromJsonAsync<DashboardResponse>("api/homes/dashboard");

            var appliance = result.Appliances.Single();
            var climateZone = appliance.ClimateZones.Single();

            return new HeatingOverview
            {
                Mode = climateZone.Preheat.Active ? "Preheat" : climateZone.Mode,
                RoomTemperature = climateZone.RoomTemperature,
                Setpoint = climateZone.Setpoint,
                OutdoorTemperature = appliance.OutdoorTemperatureInformation.OutdoorTemperature,
                NextSetpoint = climateZone.NextSetpoint,
                NextSwitchTime = climateZone.NextSwitchTime
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new HeatingOverview();
        }
    }
}

public class BaseService
{
    private readonly string _baseAddress;
    private readonly string _subscriptionKey;
    private readonly string _authBaseAddress;
    private readonly string _authScope;
    private readonly string _authRedirect;
    private readonly string _authClientId;
    private readonly string _authUsername;
    private readonly string _authPassword;
    private AuthResponse? _authToken;

    protected BaseService(IConfiguration configuration)
    {
        _baseAddress = configuration.GetValue<string>("HEATING_API_BASE_ADDRESS");
        _subscriptionKey = configuration.GetValue<string>("HEATING_API_SUBSCRIPTION_KEY");
        _authBaseAddress = configuration.GetValue<string>("HEATING_API_AUTH_BASE_ADDRESS");
        _authScope = configuration.GetValue<string>("HEATING_API_AUTH_SCOPE");
        _authRedirect = configuration.GetValue<string>("HEATING_API_AUTH_REDIRECT_URI");
        _authClientId = configuration.GetValue<string>("HEATING_API_AUTH_CLIENT_ID");
        _authUsername = configuration.GetValue<string>("HEATING_API_AUTH_USERNAME");
        _authPassword = configuration.GetValue<string>("HEATING_API_AUTH_PASSWORD");
    }

    protected async Task<HttpClient> InitializeHttpClient()
    {
        if (_authToken == null || _authToken.ExpiresOn <= TimeProvider.System.GetLocalNow().ToUnixTimeSeconds())
        {
            _authToken = await GetAuthToken();
        }

        var accessToken = _authToken.AccessToken;

        var client = new HttpClient();
        client.BaseAddress = new Uri(_baseAddress);
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    private async Task<AuthResponse> GetAuthToken()
    {
        var cookies = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookies
        };

        var state = CryptoRandom.CreateUniqueId();
        var codeVerifier = CryptoRandom.CreateUniqueId(64);
        var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = Base64Url.Encode(challengeBytes);

        var scope = UrlEncoder.Default.Encode(_authScope);
        var redirect = UrlEncoder.Default.Encode(_authRedirect);

        using var authClient1 = new HttpClient(handler);
        var response1 = await authClient1.GetAsync($"{_authBaseAddress}/bdrb2cprod.onmicrosoft.com/oauth2/v2.0/authorize?response_type=code&client_id={_authClientId}&scope={scope}&state={state}&p=B2C_1A_RPSignUpSignInNewRoomV3.1&brand=remeha&prompt=login&signup=False&redirect_uri={redirect}&lang=en&nonce=defaultNonce&code_challenge={codeChallenge}&code_challenge_method=S256");

        UriBuilder builder = new UriBuilder(response1.RequestMessage.RequestUri);

        var request_id = response1.Headers.GetValues("X-Request-ID").FirstOrDefault();

        var csrf_token = cookies.GetCookies(new Uri(_authBaseAddress)).Cast<Cookie>().FirstOrDefault(x => x.Name == "x-ms-cpim-csrf")?.Value;
        var headers = response1.Headers.GetValues("Set-Cookie");

        var state_properties_json = $$$"""{"TID":"{{{request_id}}}"}""";
        var state_properties = Convert.ToBase64String(Encoding.ASCII.GetBytes(state_properties_json));

        using var authClient2 = new HttpClient(handler);
        authClient2.DefaultRequestHeaders.Add("x-csrf-token", csrf_token);
        var httpContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("request_type", "RESPONSE"),
            new KeyValuePair<string, string>("signInName", _authUsername),
            new KeyValuePair<string, string>("password", _authPassword)
        });
        var response2 = await authClient2.PostAsync($"{_authBaseAddress}/bdrb2cprod.onmicrosoft.com/B2C_1A_RPSignUpSignInNewRoomv3.1/SelfAsserted?tx=StateProperties={state_properties}&p=B2C_1A_RPSignUpSignInNewRoomv3.1", httpContent);

        var authClient3 = new HttpClient(handler);
        var response3 = await authClient3.GetAsync($"{_authBaseAddress}/bdrb2cprod.onmicrosoft.com/B2C_1A_RPSignUpSignInNewRoomv3.1/api/CombinedSigninAndSignup/confirmed?rememberMe=false&csrf_token={csrf_token}&tx=StateProperties={state_properties}&p=B2C_1A_RPSignUpSignInNewRoomv3.1");

        var location = response3.Headers.Location;
        var queryParams = location.Query.Split(new char[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries);
        var authCode = queryParams.SingleOrDefault(x => x.StartsWith("code="))?.Replace("code=", "");

        var authClient4 = new HttpClient();
        var authContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", authCode),
            new KeyValuePair<string, string>("redirect_uri", _authRedirect),
            new KeyValuePair<string, string>("code_verifier", codeVerifier),
            new KeyValuePair<string, string>("client_id", _authClientId)
        });
        var response4 = await authClient4.PostAsync($"{_authBaseAddress}/bdrb2cprod.onmicrosoft.com/oauth2/v2.0/token?p=B2C_1A_RPSignUpSignInNewRoomV3.1", authContent);

        var token = await response4.Content.ReadFromJsonAsync<AuthResponse>();
        return token;
    }
}

public class AuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("expires_on")]
    public long ExpiresOn { get; set; }
}

public class DashboardResponse
{
    [JsonPropertyName("appliances")]
    public List<Appliance> Appliances { get; set; }
}

public class Appliance
{
    [JsonPropertyName("activeThermalMode")]
    public string ActiveThermalMode { get; set; }

    [JsonPropertyName("outdoorTemperatureInformation")]
    public TemperatureInformation OutdoorTemperatureInformation { get; set; }

    [JsonPropertyName("climateZones")]
    public List<ClimateZone> ClimateZones { get; set; }
}

public class TemperatureInformation
{
    [JsonPropertyName("cloudOutdoorTemperature")]
    public decimal OutdoorTemperature { get; set; }
}

public class ClimateZone
{
    [JsonPropertyName("roomTemperature")]
    public decimal RoomTemperature { get; set; }

    [JsonPropertyName("zoneMode")]
    public string Mode { get; set; }

    [JsonPropertyName("setpoint")]
    public decimal Setpoint { get; set; }

    [JsonPropertyName("nextSetpoint")]
    public decimal NextSetpoint { get; set; }

    [JsonPropertyName("nextSwitchTime")]
    public DateTime NextSwitchTime { get; set; }

    [JsonPropertyName("preHeat")]
    public Preheat Preheat { get; set; }
}

public class Preheat
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }
}