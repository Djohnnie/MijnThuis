using System.Text.Json.Serialization;

namespace GreeAcController.Gree;

/// <summary>
/// Generic outer envelope used for every UDP message exchanged with a Gree device.
/// </summary>
public class GreeEnvelope
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = "pack";

    [JsonPropertyName("i")]
    public int I { get; set; }

    [JsonPropertyName("uid")]
    public int Uid { get; set; }

    [JsonPropertyName("cid")]
    public string Cid { get; set; } = "app";

    [JsonPropertyName("tcid")]
    public string Tcid { get; set; } = string.Empty;

    [JsonPropertyName("pack")]
    public string? Pack { get; set; }

    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; set; }
}

/// <summary>
/// The plain "scan" request, sent unencrypted as a UDP broadcast.
/// </summary>
public class ScanRequest
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = "scan";
}

/// <summary>
/// Decrypted content of a scan response's "pack" field.
/// </summary>
public class ScanResponsePack
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("cid")]
    public string Cid { get; set; } = string.Empty;

    [JsonPropertyName("mac")]
    public string Mac { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string? Model { get; set; }
}

/// <summary>
/// Encrypted content sent to bind to a device.
/// </summary>
public class BindRequestPack
{
    [JsonPropertyName("mac")]
    public string Mac { get; set; } = string.Empty;

    [JsonPropertyName("t")]
    public string Type { get; set; } = "bind";

    [JsonPropertyName("uid")]
    public int Uid { get; set; }
}

/// <summary>
/// Decrypted content of a successful bind response.
/// </summary>
public class BindResponsePack
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("mac")]
    public string Mac { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("r")]
    public int Result { get; set; }
}

/// <summary>
/// Encrypted content of a status request ("give me the values for these columns").
/// </summary>
public class StatusRequestPack
{
    [JsonPropertyName("cols")]
    public List<string> Cols { get; set; } = new();

    [JsonPropertyName("mac")]
    public string Mac { get; set; } = string.Empty;

    [JsonPropertyName("t")]
    public string Type { get; set; } = "status";
}

/// <summary>
/// Decrypted content of a status response.
/// </summary>
public class StatusResponsePack
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("mac")]
    public string Mac { get; set; } = string.Empty;

    [JsonPropertyName("r")]
    public int Result { get; set; }

    [JsonPropertyName("cols")]
    public List<string> Cols { get; set; } = new();

    [JsonPropertyName("dat")]
    public List<int> Dat { get; set; } = new();
}

/// <summary>
/// Encrypted content of a command (control) request.
/// </summary>
public class CommandRequestPack
{
    [JsonPropertyName("opt")]
    public List<string> Opt { get; set; } = new();

    [JsonPropertyName("p")]
    public List<int> P { get; set; } = new();

    [JsonPropertyName("t")]
    public string Type { get; set; } = "cmd";
}

/// <summary>
/// Decrypted content of a command response.
/// </summary>
public class CommandResponsePack
{
    [JsonPropertyName("t")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("mac")]
    public string Mac { get; set; } = string.Empty;

    [JsonPropertyName("r")]
    public int Result { get; set; }

    [JsonPropertyName("opt")]
    public List<string> Opt { get; set; } = new();

    [JsonPropertyName("val")]
    public List<int>? Val { get; set; }

    [JsonPropertyName("p")]
    public List<int>? P { get; set; }
}

/// <summary>
/// Describes a local IPv4 network the machine is actually routable on (has a gateway),
/// used to pick the right subnet to sweep for device discovery.
/// </summary>
public record LocalNetworkInfo(string InterfaceName, System.Net.IPAddress Address, System.Net.IPAddress SubnetMask, System.Net.IPAddress Broadcast)
{
    public override string ToString() => $"{InterfaceName}: {Address} (mask {SubnetMask})";
}

/// <summary>
/// Which AES mode a device's protocol messages use. Older units use ECB; newer firmware
/// uses GCM (indicated by the presence of a "tag" field in scan/bind responses).
/// </summary>
public enum GreeEncryptionMode
{
    Ecb,
    Gcm
}

/// <summary>
/// A discovered and (optionally) bound Gree device.
/// </summary>
public class GreeDevice
{
    public required string Mac { get; init; }
    public required string Name { get; init; }
    public required System.Net.IPAddress IpAddress { get; init; }
    public string? Model { get; init; }
    public string? Key { get; set; }
    public GreeEncryptionMode EncryptionMode { get; set; } = GreeEncryptionMode.Ecb;

    public bool IsBound => !string.IsNullOrEmpty(Key);

    public override string ToString() => $"{Name} ({Mac}) @ {IpAddress}";
}
