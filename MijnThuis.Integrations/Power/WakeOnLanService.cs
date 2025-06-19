using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MijnThuis.Integrations.Power;

public interface IWakeOnLanService
{
    Task Wake();
}

public class WakeOnLanService : BaseService, IWakeOnLanService
{
    private readonly string _wakeOnLanMacAddress;
    private readonly string _wakeOnLanIpAddress;
    private readonly ILogger _logger;

    public WakeOnLanService(IConfiguration configuration, ILogger<WakeOnLanService> logger) : base(configuration)
    {
        _wakeOnLanMacAddress = configuration.GetValue<string>("WAKE_ON_LAN_MAC_ADDRESS");
        _wakeOnLanIpAddress = configuration.GetValue<string>("WAKE_ON_LAN_IP_ADDRESS");
        _logger = logger;
    }

    public async Task Wake()
    {
        await WakeOnLan(_wakeOnLanMacAddress, _wakeOnLanIpAddress);
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