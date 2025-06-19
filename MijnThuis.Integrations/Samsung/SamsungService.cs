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

    Task TurnOnTheFrame();

    Task TurnOffTheFrame();
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

    public async Task TurnOnTheFrame()
    {
        var macAddress = _configuration.GetValue<string>("THE_FRAME_MAC_ADDRESS");
        var ipAddress = _configuration.GetValue<string>("THE_FRAME_IP_ADDRESS");

        await WakeOnLan(macAddress, ipAddress);

        // Wait for the TV to boot up
        await Task.Delay(10000);

        // Press the return key to hide the home menu
        await PressKey("Click", "KEY_RETURN");
    }

    public async Task TurnOffTheFrame()
    {
        // Keep the power key pressed to fully turn off the TV
        await PressKey("Press", "KEY_POWER");
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

    private async Task PressKey(string command, string key)
    {
        var ipAddress = _configuration.GetValue<string>("THE_FRAME_IP_ADDRESS");
        var token = _configuration.GetValue<string>("THE_FRAME_TOKEN");

        byte[] appNameBytes = Encoding.UTF8.GetBytes("SamsungRemoteDemo");
        var appName = Convert.ToBase64String(appNameBytes);

        var webSocketUrl = $"wss://192.168.10.116:8002/api/v2/channels/samsung.remote.control?name={appName}&token={token}";

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

    private async Task WakeOnLan(string macAddress, string ipAddress)
    {
        byte[] magicPacket = BuildMagicPacket(macAddress);
        await SendWakeOnLan(IPAddress.Any, IPAddress.Parse(ipAddress), magicPacket);
    }

    private byte[] BuildMagicPacket(string macAddress) // MacAddress in any standard HEX format
    {
        macAddress = Regex.Replace(macAddress, "[: -]", "");
        byte[] macBytes = Convert.FromHexString(macAddress);

        IEnumerable<byte> header = Enumerable.Repeat((byte)0xff, 6); //First 6 times 0xff
        IEnumerable<byte> data = Enumerable.Repeat(macBytes, 16).SelectMany(m => m); // then 16 times MacAddress
        return header.Concat(data).ToArray();
    }

    private async Task SendWakeOnLan(IPAddress localIpAddress, IPAddress multicastIpAddress, byte[] magicPacket)
    {
        using UdpClient client = new(new IPEndPoint(localIpAddress, 0));
        await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(multicastIpAddress, 9));
    }
}