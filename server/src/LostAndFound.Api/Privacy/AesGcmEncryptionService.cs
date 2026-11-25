using System.Security.Cryptography;
using System.Text;
using LostAndFound.Api.Options;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Privacy;

/// <summary>
/// Encrypts and decrypts private string fields using AES-256-GCM with a random 12-byte nonce per value.
/// </summary>
public sealed class AesGcmEncryptionService : IEncryptionService
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private readonly byte[] _key;

    public AesGcmEncryptionService(IOptions<EncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _key = Convert.FromBase64String(options.Value.Key);
        if (_key.Length != EncryptionOptionsValidator.KeySizeBytes)
        {
            throw new InvalidOperationException("Encryption key must be 32 bytes.");
        }
    }

    public string Encrypt(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var payload = new byte[NonceSizeBytes + plaintextBytes.Length + TagSizeBytes];

        var nonce = payload.AsSpan(0, NonceSizeBytes);
        var ciphertext = payload.AsSpan(NonceSizeBytes, plaintextBytes.Length);
        var tag = payload.AsSpan(NonceSizeBytes + plaintextBytes.Length, TagSizeBytes);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return Convert.ToBase64String(payload);
    }

    public string Decrypt(string ciphertext)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        var payload = Convert.FromBase64String(ciphertext);
        if (payload.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Ciphertext payload is too short.");
        }

        var cipherLength = payload.Length - NonceSizeBytes - TagSizeBytes;
        var nonce = payload.AsSpan(0, NonceSizeBytes);
        var cipherBytes = payload.AsSpan(NonceSizeBytes, cipherLength);
        var tag = payload.AsSpan(NonceSizeBytes + cipherLength, TagSizeBytes);
        var plaintextBytes = new byte[cipherLength];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, cipherBytes, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
