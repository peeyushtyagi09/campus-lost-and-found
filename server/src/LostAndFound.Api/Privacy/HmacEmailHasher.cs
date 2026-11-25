using System.Security.Cryptography;
using System.Text;
using LostAndFound.Api.Options;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Privacy;

/// <summary>
/// Computes a keyed SHA-256 lookup hash for normalized emails using a domain-separated key derived from Encryption:Key.
/// </summary>
public sealed class HmacEmailHasher : IEmailHasher
{
    private static readonly byte[] DomainInfo = "campus-lost-and-found:email-hash:v1"u8.ToArray();
    private readonly byte[] _pepperKey;

    public HmacEmailHasher(IOptions<EncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var masterKey = Convert.FromBase64String(options.Value.Key);
        _pepperKey = HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            masterKey,
            outputLength: 32,
            salt: null,
            info: DomainInfo);
    }

    public string Normalize(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToLowerInvariant();
    }

    public string Hash(string email)
    {
        var normalized = Normalize(email);
        var emailBytes = Encoding.UTF8.GetBytes(normalized);
        var hash = HMACSHA256.HashData(_pepperKey, emailBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
