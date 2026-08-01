using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;

namespace GreeAcController.Gree;

/// <summary>
/// UDP client implementing the reverse-engineered Gree AC protocol
/// (see https://github.com/tomikaa87/gree-remote for the protocol description).
/// Devices listen on UDP port 7000.
/// </summary>
public class GreeClient
{
    private const int DevicePort = 7000;
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(3);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    /// <summary>
    /// Broadcasts a "scan" packet on every local IPv4 network (using each interface's
    /// own subnet broadcast address as well as the general 255.255.255.255 address)
    /// and collects responses from any Gree device that answers within the given time window.
    /// This can fail to find devices on networks with client/AP isolation or when the
    /// broadcast is otherwise blocked (e.g. some Wi-Fi routers, VLANs, firewalls) - in that
    /// case use <see cref="DiscoverBySweepAsync"/> instead.
    /// </summary>
    public async Task<List<GreeDevice>> DiscoverAsync(TimeSpan? scanDuration = null, CancellationToken cancellationToken = default)
    {
        scanDuration ??= TimeSpan.FromSeconds(3);

        var devices = new List<GreeDevice>();
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        DisableConnectionResetReporting(udp.Client);

        var scanRequest = JsonSerializer.Serialize(new ScanRequest(), _jsonOptions);
        var scanBytes = System.Text.Encoding.UTF8.GetBytes(scanRequest);

        // Send to the generic broadcast address plus every local interface's own
        // subnet broadcast address, since 255.255.255.255 alone doesn't always
        // get routed out correctly when multiple network adapters are present.
        foreach (var broadcastAddress in GetBroadcastAddresses())
        {
            try
            {
                await udp.SendAsync(scanBytes, scanBytes.Length, new IPEndPoint(broadcastAddress, DevicePort));
            }
            catch (SocketException)
            {
                // Ignore adapters that refuse to send (e.g. disconnected virtual adapters).
            }
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(scanDuration.Value);

        await ReceiveLoopAsync(udp, devices, cts.Token);

        return devices;
    }

    /// <summary>
    /// Fallback discovery that sends a unicast "scan" packet to every host address in the
    /// given local /24 subnet (e.g. "192.168.1.0/24"). Slower than a broadcast, but works
    /// even when the network blocks broadcast/multicast traffic (client isolation, VLANs, etc.).
    /// If <paramref name="subnetBaseAddress"/> is null, it is inferred from the local machine's
    /// first active IPv4 network interface.
    /// </summary>
    public async Task<List<GreeDevice>> DiscoverBySweepAsync(string? subnetBaseAddress = null, TimeSpan? perHostTimeout = null, CancellationToken cancellationToken = default)
    {
        perHostTimeout ??= TimeSpan.FromSeconds(4);

        var baseAddress = subnetBaseAddress is not null
            ? IPAddress.Parse(subnetBaseAddress)
            : GetLocalIPv4Addresses().FirstOrDefault()
                ?? throw new InvalidOperationException("Could not determine a local IPv4 address. Please supply a subnet manually.");

        var baseOctets = baseAddress.GetAddressBytes();
        var devices = new List<GreeDevice>();

        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        DisableConnectionResetReporting(udp.Client);

        var scanRequest = JsonSerializer.Serialize(new ScanRequest(), _jsonOptions);
        var scanBytes = System.Text.Encoding.UTF8.GetBytes(scanRequest);

        for (var host = 1; host <= 254; host++)
        {
            var target = new IPAddress([baseOctets[0], baseOctets[1], baseOctets[2], (byte)host]);
            await udp.SendAsync(scanBytes, scanBytes.Length, new IPEndPoint(target, DevicePort));
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(perHostTimeout.Value);

        await ReceiveLoopAsync(udp, devices, cts.Token);

        return devices;
    }

    /// <summary>
    /// Repeatedly receives UDP datagrams until cancelled, tolerating individual receive
    /// errors (e.g. ICMP port-unreachable resets from hosts that aren't listening on
    /// port 7000, which Windows surfaces as a SocketException on the next receive call).
    /// </summary>
    private async Task ReceiveLoopAsync(UdpClient udp, List<GreeDevice> devices, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await udp.ReceiveAsync(cancellationToken);
                TryAddDeviceFromScanResponse(devices, result.Buffer, result.RemoteEndPoint.Address);
            }
            catch (OperationCanceledException)
            {
                // Expected once the scan window elapses.
                break;
            }
            catch (SocketException)
            {
                // A previously sent datagram was rejected (e.g. ICMP port-unreachable from a
                // host with nothing listening on port 7000). Harmless for discovery; keep listening.
            }
        }
    }

    /// <summary>
    /// On Windows, an ICMP "port unreachable" response to a previously sent UDP datagram
    /// causes the next receive on that socket to fail with WSAECONNRESET (10054), even
    /// though this isn't a fatal error for a connectionless UDP scan/sweep. This disables
    /// that behavior via the SIO_UDP_CONNRESET control code. No-op on non-Windows platforms.
    /// </summary>
    private static void DisableConnectionResetReporting(Socket socket)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int SioUdpConnReset = -1744830452;

        try
        {
            socket.IOControl(SioUdpConnReset, [0], null);
        }
        catch (SocketException)
        {
            // Best-effort; ignore if unsupported.
        }
    }

    private void TryAddDeviceFromScanResponse(List<GreeDevice> devices, byte[] buffer, IPAddress remoteAddress)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(buffer);
            var envelope = JsonSerializer.Deserialize<GreeEnvelope>(json, _jsonOptions);

            if (envelope?.Pack is null)
            {
                return;
            }

            // A "tag" field present means this device uses the newer GCM encryption variant.
            var encryptionMode = envelope.Tag is not null ? GreeEncryptionMode.Gcm : GreeEncryptionMode.Ecb;
            var decrypted = encryptionMode == GreeEncryptionMode.Gcm
                ? GreeCrypto.DecryptGcm(envelope.Pack, envelope.Tag!, GreeCrypto.GenericGcmKey)
                : GreeCrypto.Decrypt(envelope.Pack, GreeCrypto.GenericKey);

            var scanResponse = JsonSerializer.Deserialize<ScanResponsePack>(decrypted, _jsonOptions);

            if (scanResponse is null || scanResponse.Type != "dev")
            {
                return;
            }

            if (devices.Any(d => d.Mac == scanResponse.Mac))
            {
                return;
            }

            devices.Add(new GreeDevice
            {
                Mac = scanResponse.Mac,
                Name = string.IsNullOrWhiteSpace(scanResponse.Name) ? scanResponse.Mac : scanResponse.Name,
                Model = scanResponse.Model,
                IpAddress = remoteAddress,
                EncryptionMode = encryptionMode
            });
        }
        catch (Exception)
        {
            // Not every UDP packet received on port 7000 is a valid Gree scan response
            // (e.g. malformed data, or noise from other devices); safely ignore those.
        }
    }

    private static IEnumerable<IPAddress> GetBroadcastAddresses()
    {
        yield return IPAddress.Broadcast;

        foreach (var network in GetLocalNetworks())
        {
            yield return network.Broadcast;
        }
    }

    private static IEnumerable<IPAddress> GetLocalIPv4Addresses() =>
        GetLocalNetworks().Select(n => n.Address);

    /// <summary>
    /// Enumerates the local IPv4 networks that are actually routable (i.e. have a default
    /// gateway configured), skipping virtual adapters such as Hyper-V/WSL internal switches
    /// which typically have no gateway and would otherwise be picked up first and cause
    /// discovery to scan the wrong subnet entirely.
    /// </summary>
    public static List<LocalNetworkInfo> GetLocalNetworks()
    {
        var networks = new List<LocalNetworkInfo>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up ||
                nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
            {
                continue;
            }

            var ipProperties = nic.GetIPProperties();

            var hasGateway = ipProperties.GatewayAddresses
                .Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any));

            if (!hasGateway)
            {
                // Skip adapters without a real gateway (Hyper-V/WSL virtual switches, disconnected
                // adapters, etc.) - these aren't the network the AC unit is actually on.
                continue;
            }

            foreach (var unicast in ipProperties.UnicastAddresses)
            {
                if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                {
                    continue;
                }

                var addressBytes = unicast.Address.GetAddressBytes();
                var maskBytes = unicast.IPv4Mask?.GetAddressBytes();

                if (maskBytes is null || maskBytes.Length != 4)
                {
                    continue;
                }

                var broadcastBytes = new byte[4];
                for (var i = 0; i < 4; i++)
                {
                    broadcastBytes[i] = (byte)(addressBytes[i] | ~maskBytes[i]);
                }

                networks.Add(new LocalNetworkInfo(
                    nic.Name,
                    unicast.Address,
                    unicast.IPv4Mask!,
                    new IPAddress(broadcastBytes)));
            }
        }

        return networks;
    }

    /// <summary>
    /// Binds to a device to obtain its device-specific AES key, required for status queries
    /// and control commands. Tries the encryption mode detected during scanning first (or ECB
    /// if unknown, e.g. for manually entered devices), falling back to the other mode if the
    /// device doesn't respond - newer Gree firmware uses AES-GCM instead of the older AES-ECB.
    /// Sets <see cref="GreeDevice.Key"/> and <see cref="GreeDevice.EncryptionMode"/> on success.
    /// </summary>
    public async Task<bool> BindAsync(GreeDevice device, CancellationToken cancellationToken = default)
    {
        var bindPack = new BindRequestPack { Mac = device.Mac };
        var packJson = JsonSerializer.Serialize(bindPack, _jsonOptions);

        var modesToTry = device.EncryptionMode == GreeEncryptionMode.Gcm
            ? [GreeEncryptionMode.Gcm, GreeEncryptionMode.Ecb]
            : new[] { GreeEncryptionMode.Ecb, GreeEncryptionMode.Gcm };

        foreach (var mode in modesToTry)
        {
            var genericKey = mode == GreeEncryptionMode.Gcm ? GreeCrypto.GenericGcmKey : GreeCrypto.GenericKey;
            var envelope = BuildEnvelope(device.Mac, 1, packJson, genericKey, mode);

            var responseEnvelope = await SendAndReceiveAsync(device.IpAddress, envelope, cancellationToken);
            if (responseEnvelope?.Pack is null)
            {
                continue; // No response (timeout) - try the other encryption mode.
            }

            try
            {
                var decrypted = DecryptEnvelope(responseEnvelope, genericKey, mode);
                var bindResponse = JsonSerializer.Deserialize<BindResponsePack>(decrypted, _jsonOptions);

                if (bindResponse is null ||
                    !string.Equals(bindResponse.Type, "bindok", StringComparison.OrdinalIgnoreCase) ||
                    bindResponse.Result != 200)
                {
                    continue;
                }

                device.Key = bindResponse.Key;
                device.EncryptionMode = mode;
                return true;
            }
            catch (CryptographicException)
            {
                // Response couldn't be decrypted/authenticated with this mode's key; try the other one.
            }
        }

        return false;
    }

    /// <summary>
    /// Requests the values of the given columns (parameters) from a bound device.
    /// Returns a dictionary mapping column name to its raw integer value.
    /// </summary>
    public async Task<Dictionary<string, int>> GetStatusAsync(GreeDevice device, IEnumerable<string> columns, CancellationToken cancellationToken = default)
    {
        EnsureBound(device);

        var statusPack = new StatusRequestPack { Mac = device.Mac, Cols = columns.ToList() };
        var packJson = JsonSerializer.Serialize(statusPack, _jsonOptions);
        var envelope = BuildEnvelope(device.Mac, 0, packJson, device.Key!, device.EncryptionMode);

        var responseEnvelope = await SendAndReceiveAsync(device.IpAddress, envelope, cancellationToken);
        if (responseEnvelope?.Pack is null)
        {
            throw new InvalidOperationException("No response received from the device.");
        }

        var decrypted = DecryptEnvelope(responseEnvelope, device.Key!, device.EncryptionMode);
        var statusResponse = JsonSerializer.Deserialize<StatusResponsePack>(decrypted, _jsonOptions);

        if (statusResponse is null || statusResponse.Type != "dat")
        {
            throw new InvalidOperationException("Unexpected response from the device.");
        }

        var result = new Dictionary<string, int>();
        for (var i = 0; i < statusResponse.Cols.Count && i < statusResponse.Dat.Count; i++)
        {
            result[statusResponse.Cols[i]] = statusResponse.Dat[i];
        }

        return result;
    }

    /// <summary>
    /// Sends one or more parameter changes to a bound device.
    /// </summary>
    public async Task<bool> SetParametersAsync(GreeDevice device, IReadOnlyDictionary<string, int> parameters, CancellationToken cancellationToken = default)
    {
        EnsureBound(device);

        var commandPack = new CommandRequestPack
        {
            Opt = parameters.Keys.ToList(),
            P = parameters.Values.ToList()
        };

        var packJson = JsonSerializer.Serialize(commandPack, _jsonOptions);
        var envelope = BuildEnvelope(device.Mac, 0, packJson, device.Key!, device.EncryptionMode);

        var responseEnvelope = await SendAndReceiveAsync(device.IpAddress, envelope, cancellationToken);
        if (responseEnvelope?.Pack is null)
        {
            return false;
        }

        var decrypted = DecryptEnvelope(responseEnvelope, device.Key!, device.EncryptionMode);
        var commandResponse = JsonSerializer.Deserialize<CommandResponsePack>(decrypted, _jsonOptions);

        return commandResponse is { Type: "res", Result: 200 };
    }

    /// <summary>
    /// Builds a "pack"-type envelope, encrypting the given JSON payload with the
    /// appropriate scheme (ECB or GCM) and attaching the "tag" field for GCM.
    /// </summary>
    private static GreeEnvelope BuildEnvelope(string mac, int i, string packJson, string key, GreeEncryptionMode mode)
    {
        var envelope = new GreeEnvelope
        {
            Type = "pack",
            I = i,
            Uid = 0,
            Cid = "app",
            Tcid = mac
        };

        if (mode == GreeEncryptionMode.Gcm)
        {
            var (pack, tag) = GreeCrypto.EncryptGcm(packJson, key);
            envelope.Pack = pack;
            envelope.Tag = tag;
        }
        else
        {
            envelope.Pack = GreeCrypto.Encrypt(packJson, key);
        }

        return envelope;
    }

    /// <summary>
    /// Decrypts a response envelope's "pack" field using the appropriate scheme.
    /// </summary>
    private static string DecryptEnvelope(GreeEnvelope envelope, string key, GreeEncryptionMode mode)
    {
        if (mode == GreeEncryptionMode.Gcm)
        {
            if (envelope.Tag is null)
            {
                throw new InvalidOperationException("Expected a GCM 'tag' field in the response but none was present.");
            }

            return GreeCrypto.DecryptGcm(envelope.Pack!, envelope.Tag, key);
        }

        return GreeCrypto.Decrypt(envelope.Pack!, key);
    }

    private static void EnsureBound(GreeDevice device)
    {
        if (!device.IsBound)
        {
            throw new InvalidOperationException($"Device '{device.Name}' is not bound yet. Call BindAsync first.");
        }
    }

    private async Task<GreeEnvelope?> SendAndReceiveAsync(IPAddress address, GreeEnvelope envelope, CancellationToken cancellationToken)
    {
        using var udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        DisableConnectionResetReporting(udp.Client);

        var requestJson = JsonSerializer.Serialize(envelope, _jsonOptions);
        var requestBytes = System.Text.Encoding.UTF8.GetBytes(requestJson);

        var endpoint = new IPEndPoint(address, DevicePort);
        await udp.SendAsync(requestBytes, requestBytes.Length, endpoint);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(ReceiveTimeout);

        try
        {
            var result = await udp.ReceiveAsync(cts.Token);
            var json = System.Text.Encoding.UTF8.GetString(result.Buffer);
            return JsonSerializer.Deserialize<GreeEnvelope>(json, _jsonOptions);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (SocketException)
        {
            // ICMP port-unreachable surfaced as a connection reset; treat as "no response".
            return null;
        }
    }
}
