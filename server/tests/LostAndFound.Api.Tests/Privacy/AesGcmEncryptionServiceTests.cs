using System.Security.Cryptography;
using LostAndFound.Api.Options;
using LostAndFound.Api.Privacy;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Tests.Privacy;

public sealed class AesGcmEncryptionServiceTests
{
    private static AesGcmEncryptionService CreateService(byte keySeed = 11)
    {
        var key = Enumerable.Repeat(keySeed, 32).ToArray();
        var options = Microsoft.Extensions.Options.Options.Create(new EncryptionOptions
        {
            Key = Convert.ToBase64String(key)
        });
        return new AesGcmEncryptionService(options);
    }

    [Fact]
    public void RoundTrip_ReturnsOriginalPlaintext()
    {
        var service = CreateService();
        const string secret = "Serial: SN-99412-CAMPUS; scratch on back cover";

        var encrypted = service.Encrypt(secret);
        var decrypted = service.Decrypt(encrypted);

        encrypted.Should().NotBe(secret);
        decrypted.Should().Be(secret);
    }

    [Fact]
    public void SamePlaintext_ProducesDifferentCiphertextDueToRandomNonce()
    {
        var service = CreateService();
        const string secret = "student@campus.edu";

        var first = service.Encrypt(secret);
        var second = service.Encrypt(secret);

        first.Should().NotBe(second);
        service.Decrypt(first).Should().Be(secret);
        service.Decrypt(second).Should().Be(secret);
    }

    [Fact]
    public void TamperedCiphertext_ThrowsCryptographicException()
    {
        var service = CreateService();
        var payload = Convert.FromBase64String(service.Encrypt("sensitive-contact-phone"));
        payload[^1] ^= 0xFF;
        var tampered = Convert.ToBase64String(payload);

        var act = () => service.Decrypt(tampered);

        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void WrongKey_ThrowsCryptographicException()
    {
        var encryptor = CreateService(11);
        var wrongDecryptor = CreateService(99);

        var ciphertext = encryptor.Encrypt("private-notes");

        var act = () => wrongDecryptor.Decrypt(ciphertext);

        act.Should().Throw<CryptographicException>();
    }
}
