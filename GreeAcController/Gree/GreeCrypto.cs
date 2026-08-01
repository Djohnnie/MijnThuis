using System.Security.Cryptography;
using System.Text;

namespace GreeAcController.Gree;

/// <summary>
/// Handles encryption and decryption for the Gree "pack" payloads, as described by the
/// reverse-engineered protocol in https://github.com/tomikaa87/gree-remote.
/// Older units use AES-128/ECB/PKCS7; newer firmware uses AES-128/GCM with a fixed
/// nonce/associated-data, distinguishable by the presence of a "tag" field in messages.
/// </summary>
public static class GreeCrypto
{
    /// <summary>
    /// The generic AES key shared by all devices for ECB-mode scanning and binding.
    /// </summary>
    public const string GenericKey = "a3K8Bx%2r8Y7#xDh";

    /// <summary>
    /// The generic AES key shared by all devices for GCM-mode scanning and binding.
    /// </summary>
    public const string GenericGcmKey = "{yxAHAY_Lm6pbC/<";

    // Fixed nonce and associated data used by the Gree GCM protocol variant for every message.
    private static readonly byte[] GcmNonce = [0x54, 0x40, 0x78, 0x44, 0x49, 0x67, 0x5a, 0x51, 0x6c, 0x5e, 0x63, 0x13];
    private static readonly byte[] GcmAssociatedData = Encoding.UTF8.GetBytes("qualcomm-test");
    private const int GcmTagSizeBytes = 16;

    public static string Encrypt(string plainJson, string key)
    {
        using var aes = CreateAes(key);
        using var encryptor = aes.CreateEncryptor();

        var plainBytes = Encoding.UTF8.GetBytes(plainJson);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        return Convert.ToBase64String(cipherBytes);
    }

    public static string Decrypt(string base64Cipher, string key)
    {
        using var aes = CreateAes(key);
        using var decryptor = aes.CreateDecryptor();

        var cipherBytes = Convert.FromBase64String(base64Cipher);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }

    /// <summary>
    /// Encrypts a JSON payload using AES-128/GCM, returning the base64-encoded ciphertext
    /// and the base64-encoded authentication tag (sent separately as the "tag" field).
    /// </summary>
    public static (string Pack, string Tag) EncryptGcm(string plainJson, string key)
    {
        using var aesGcm = new AesGcm(Encoding.UTF8.GetBytes(key), GcmTagSizeBytes);

        var plainBytes = Encoding.UTF8.GetBytes(plainJson);
        var cipherBytes = new byte[plainBytes.Length];
        var tagBytes = new byte[GcmTagSizeBytes];

        aesGcm.Encrypt(GcmNonce, plainBytes, cipherBytes, tagBytes, GcmAssociatedData);

        return (Convert.ToBase64String(cipherBytes), Convert.ToBase64String(tagBytes));
    }

    /// <summary>
    /// Decrypts an AES-128/GCM "pack"/"tag" pair.
    /// </summary>
    public static string DecryptGcm(string base64Cipher, string base64Tag, string key)
    {
        using var aesGcm = new AesGcm(Encoding.UTF8.GetBytes(key), GcmTagSizeBytes);

        var cipherBytes = Convert.FromBase64String(base64Cipher);
        var tagBytes = Convert.FromBase64String(base64Tag);
        var plainBytes = new byte[cipherBytes.Length];

        aesGcm.Decrypt(GcmNonce, cipherBytes, tagBytes, plainBytes, GcmAssociatedData);

        // The reference implementation strips stray 0xFF filler bytes from the decrypted output.
        var cleaned = plainBytes.Where(b => b != 0xFF).ToArray();
        return Encoding.UTF8.GetString(cleaned);
    }

    private static Aes CreateAes(string key)
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = Encoding.UTF8.GetBytes(key);

        return aes;
    }
}
