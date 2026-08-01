using System.Text.Json.Serialization;

namespace MijnThuis.Integrations.Airconditioning.Gree;

/// <summary>
/// Which AES mode a device's protocol messages use. Older units use ECB; newer firmware
/// uses GCM (indicated by the presence of a "tag" field in bind/status/command responses).
/// </summary>
internal enum GreeEncryptionMode
{
    Ecb,
    Gcm
}

/// <summary>
/// Generic outer envelope used for every UDP message exchanged with a Gree device.
/// </summary>
internal class GreeEnvelope
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
/// Encrypted content sent to bind to a device.
/// </summary>
internal class BindRequestPack
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
internal class BindResponsePack
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
internal class StatusRequestPack
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
internal class StatusResponsePack
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
internal class CommandRequestPack
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
internal class CommandResponsePack
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
/// A Gree device that has been bound (has its device-specific AES key and negotiated
/// encryption mode) and can be queried/controlled.
/// </summary>
internal class BoundGreeDevice
{
    public required string Mac { get; init; }
    public required string Key { get; init; }
    public required GreeEncryptionMode EncryptionMode { get; init; }
}
