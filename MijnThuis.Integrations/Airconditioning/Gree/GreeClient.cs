using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;

namespace MijnThuis.Integrations.Airconditioning.Gree;

/// <summary>
/// Minimal UDP client implementing the reverse-engineered Gree AC protocol
/// (see https://github.com/tomikaa87/gree-remote for the protocol description).
/// Devices listen on UDP port 7000. Only binding and, once bound, status/control
/// requests are implemented here - discovery isn't needed since the device's IP and
/// MAC address are configured explicitly for this integration.
/// </summary>
internal class GreeClient
{
    private const int DevicePort = 7000;
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(5);

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    /// <summary>
    /// Binds to a device to obtain its device-specific AES key, required for status queries
    /// and control commands. Tries the legacy ECB scheme first, falling back to the newer
    /// AES-GCM scheme used by more recent Gree firmware if there's no response.
    /// </summary>
    public async Task<BoundGreeDevice?> BindAsync(string mac, IPAddress ipAddress, CancellationToken cancellationToken = default)
    {
        var bindPack = new BindRequestPack { Mac = mac };
        var packJson = JsonSerializer.Serialize(bindPack, _jsonOptions);

        foreach (var mode in new[] { GreeEncryptionMode.Ecb, GreeEncryptionMode.Gcm })
        {
            var genericKey = mode == GreeEncryptionMode.Gcm ? GreeCrypto.GenericGcmKey : GreeCrypto.GenericKey;
            var envelope = BuildEnvelope(mac, 1, packJson, genericKey, mode);

            var responseEnvelope = await SendAndReceiveAsync(ipAddress, envelope, cancellationToken);
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

                return new BoundGreeDevice
                {
                    Mac = mac,
                    Key = bindResponse.Key,
                    EncryptionMode = mode
                };
            }
            catch (CryptographicException)
            {
                // Response couldn't be decrypted/authenticated with this mode's key; try the other one.
            }
        }

        return null;
    }

    /// <summary>
    /// Requests the values of the given columns (parameters) from a bound device.
    /// Returns a dictionary mapping column name to its raw integer value.
    /// </summary>
    public async Task<Dictionary<string, int>> GetStatusAsync(BoundGreeDevice device, IPAddress ipAddress, IEnumerable<string> columns, CancellationToken cancellationToken = default)
    {
        var statusPack = new StatusRequestPack { Mac = device.Mac, Cols = columns.ToList() };
        var packJson = JsonSerializer.Serialize(statusPack, _jsonOptions);
        var envelope = BuildEnvelope(device.Mac, 0, packJson, device.Key, device.EncryptionMode);

        var responseEnvelope = await SendAndReceiveAsync(ipAddress, envelope, cancellationToken);
        if (responseEnvelope?.Pack is null)
        {
            throw new InvalidOperationException("No response received from the air conditioning unit.");
        }

        var decrypted = DecryptEnvelope(responseEnvelope, device.Key, device.EncryptionMode);
        var statusResponse = JsonSerializer.Deserialize<StatusResponsePack>(decrypted, _jsonOptions);

        if (statusResponse is null || statusResponse.Type != "dat")
        {
            throw new InvalidOperationException("Unexpected response from the air conditioning unit.");
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
    public async Task<bool> SetParametersAsync(BoundGreeDevice device, IPAddress ipAddress, IReadOnlyDictionary<string, int> parameters, CancellationToken cancellationToken = default)
    {
        var commandPack = new CommandRequestPack
        {
            Opt = parameters.Keys.ToList(),
            P = parameters.Values.ToList()
        };

        var packJson = JsonSerializer.Serialize(commandPack, _jsonOptions);
        var envelope = BuildEnvelope(device.Mac, 0, packJson, device.Key, device.EncryptionMode);

        var responseEnvelope = await SendAndReceiveAsync(ipAddress, envelope, cancellationToken);
        if (responseEnvelope?.Pack is null)
        {
            return false;
        }

        var decrypted = DecryptEnvelope(responseEnvelope, device.Key, device.EncryptionMode);
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
            // On Windows, an ICMP "port unreachable" reply surfaces as a connection reset;
            // treat it the same as "no response".
            return null;
        }
    }

    /// <summary>
    /// On Windows, an ICMP "port unreachable" response to a previously sent UDP datagram
    /// causes the next receive on that socket to fail with WSAECONNRESET (10054), even
    /// though this isn't a fatal error here. This disables that behavior via the
    /// SIO_UDP_CONNRESET control code. No-op on non-Windows platforms.
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
}
