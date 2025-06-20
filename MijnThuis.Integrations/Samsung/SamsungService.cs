using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;

namespace MijnThuis.Integrations.Samsung;

public interface ISamsungService
{
    Task<bool> IsTheFrameOn();

    Task<string> TurnOnTheFrame(string token);

    Task<string> TurnOffTheFrame(string token);
}

public class SamsungService : ISamsungService
{
    private readonly IConfiguration _configuration;

    public SamsungService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<bool> IsTheFrameOn()
    {
        return await IsTvOn();
    }

    public async Task<string> TurnOnTheFrame(string token)
    {
        var macAddress = _configuration.GetValue<string>("THE_FRAME_MAC_ADDRESS");
        var ipAddress = _configuration.GetValue<string>("THE_FRAME_IP_ADDRESS");

        await WakeOnLan(macAddress, ipAddress);

        // Wait for the TV to boot up
        await Task.Delay(10000);

        if (string.IsNullOrEmpty(token))
        {
            token = await GetToken();
        }

        // Press the return key to hide the home menu
        await PressKey("Click", "KEY_RETURN", token);

        return token;
    }

    public async Task<string> TurnOffTheFrame(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            token = await GetToken();
        }

        // Keep the power key pressed to fully turn off the TV
        await PressKey("Press", "KEY_POWER", token);

        return token;
    }

    private async Task<bool> IsTvOn()
    {
        var ipAddress = _configuration.GetValue<string>("THE_FRAME_IP_ADDRESS");

        using HttpClient client = new HttpClient();

        string url = $"http://{ipAddress}:8001/api/v2/";
        client.Timeout = TimeSpan.FromSeconds(1);

        try
        {
            var response = await client.GetAsync(url);
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    private async Task PressKey(string command, string key, string token)
    {
        var ipAddress = _configuration.GetValue<string>("THE_FRAME_IP_ADDRESS");
        var appName = _configuration.GetValue<string>("THE_FRAME_APPNAME");

        byte[] appNameBytes = Encoding.UTF8.GetBytes(appName);
        var appNameBase64 = Convert.ToBase64String(appNameBytes);

        var webSocketUrl = $"wss://{ipAddress}:8002/api/v2/channels/samsung.remote.control?name={appNameBase64}&token={token}";

        using var ws = new ClientWebSocket();
        ws.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        await ws.ConnectAsync(new Uri(webSocketUrl), CancellationToken.None);

        var json = JsonConvert.SerializeObject(new
        {
            method = "ms.remote.control",
            parameters = new
            {
                Cmd = command,
                DataOfCmd = key,
                Option = "false",
                TypeOfRemote = "SendRemoteKey"
            }
        });
        json = json.Replace("parameters", "params");
        var data = Encoding.ASCII.GetBytes(json);

        await ws.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task<string> GetToken()
    {
        var ipAddress = _configuration.GetValue<string>("THE_FRAME_IP_ADDRESS");
        var appName = _configuration.GetValue<string>("THE_FRAME_APPNAME");

        byte[] appNameBytes = Encoding.UTF8.GetBytes(appName);
        var appNameBase64 = Convert.ToBase64String(appNameBytes);

        var webSocketUrl = $"wss://{ipAddress}:8002/api/v2/channels/samsung.remote.control?name={appNameBase64}";
        string token = string.Empty;

        using var ws = new ClientWebSocket();
        ws.Options.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        await ws.ConnectAsync(new Uri(webSocketUrl), CancellationToken.None);

        var buffer = new byte[1024];
        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
            var response = Encoding.ASCII.GetString(buffer, 0, result.Count);
            var jsonObject = JsonConvert.DeserializeObject<Rootobject>(response);

            token = jsonObject?.Data?.Token ?? string.Empty;

            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        return token;
    }

    private async Task WakeOnLan(string macAddress, string ipAddress)
    {
        byte[] magicPacket = BuildMagicPacket(macAddress);
        await SendWakeOnLan(IPAddress.Any, IPAddress.Parse(ipAddress), magicPacket);
    }

    private byte[] BuildMagicPacket(string macAddress)
    {
        macAddress = Regex.Replace(macAddress, "[: -]", "");
        byte[] macBytes = Convert.FromHexString(macAddress);

        IEnumerable<byte> header = Enumerable.Repeat((byte)0xff, 6);
        IEnumerable<byte> data = Enumerable.Repeat(macBytes, 16).SelectMany(m => m);
        return header.Concat(data).ToArray();
    }

    private async Task SendWakeOnLan(IPAddress localIpAddress, IPAddress multicastIpAddress, byte[] magicPacket)
    {
        using UdpClient client = new(new IPEndPoint(localIpAddress, 0));
        await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(multicastIpAddress, 9));
    }

    public class Rootobject
    {
        public Data Data { get; set; }
    }

    public class Data
    {
        public string Token { get; set; }
    }
}