using System.Text;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Options;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;
}

public sealed class MongoOptionsValidator : IValidateOptions<MongoOptions>
{
    public ValidateOptionsResult Validate(string? name, MongoOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail("Mongo:ConnectionString is required.");
        }

        if (string.IsNullOrWhiteSpace(options.DatabaseName))
        {
            return ValidateOptionsResult.Fail("Mongo:DatabaseName is required.");
        }

        return ValidateOptionsResult.Success;
    }
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; }
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public const int MinimumSigningKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            return ValidateOptionsResult.Fail("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            return ValidateOptionsResult.Fail("Jwt:Audience is required.");
        }

        if (Encoding.UTF8.GetByteCount(options.SigningKey) < MinimumSigningKeyBytes)
        {
            return ValidateOptionsResult.Fail("Jwt:SigningKey must be at least 32 bytes.");
        }

        if (options.ExpiryMinutes <= 0)
        {
            return ValidateOptionsResult.Fail("Jwt:ExpiryMinutes must be positive.");
        }

        return ValidateOptionsResult.Success;
    }
}

public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    public string Key { get; set; } = string.Empty;
}

public sealed class EncryptionOptionsValidator : IValidateOptions<EncryptionOptions>
{
    public const int KeySizeBytes = 32;

    public ValidateOptionsResult Validate(string? name, EncryptionOptions options)
    {
        byte[] key;
        try
        {
            key = Convert.FromBase64String(options.Key);
        }
        catch (FormatException)
        {
            return ValidateOptionsResult.Fail("Encryption:Key must be base64.");
        }

        if (key.Length != KeySizeBytes)
        {
            return ValidateOptionsResult.Fail("Encryption:Key must decode to 32 bytes.");
        }

        return ValidateOptionsResult.Success;
    }
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public const long DefaultMaxPhotoBytes = 5_242_880;

    public string UploadPath { get; set; } = string.Empty;

    public long MaxPhotoBytes { get; set; } = DefaultMaxPhotoBytes;
}

public sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.UploadPath) || options.UploadPath.Contains("..", StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail("Storage:UploadPath must be a non-empty path without '..'.");
        }

        if (options.MaxPhotoBytes <= 0 || options.MaxPhotoBytes > StorageOptions.DefaultMaxPhotoBytes)
        {
            return ValidateOptionsResult.Fail("Storage:MaxPhotoBytes must be between 1 and 5242880.");
        }

        return ValidateOptionsResult.Success;
    }
}

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public const string PolicyName = "Client";

    public string[] AllowedOrigins { get; set; } = [];
}

public sealed class CorsOptionsValidator : IValidateOptions<CorsOptions>
{
    public ValidateOptionsResult Validate(string? name, CorsOptions options)
    {
        foreach (var origin in options.AllowedOrigins)
        {
            if (origin == "*"
                || !Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return ValidateOptionsResult.Fail("Cors:AllowedOrigins entries must be absolute http or https origins.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public const string AuthPolicy = "auth";

    public const string UploadPolicy = "uploads";

    public int AuthPermitLimit { get; set; }

    public int UploadPermitLimit { get; set; }

    public int GlobalPermitLimit { get; set; }

    public int WindowSeconds { get; set; }
}

public sealed class RateLimitOptionsValidator : IValidateOptions<RateLimitOptions>
{
    public ValidateOptionsResult Validate(string? name, RateLimitOptions options)
    {
        if (options.AuthPermitLimit <= 0
            || options.UploadPermitLimit <= 0
            || options.GlobalPermitLimit <= 0
            || options.WindowSeconds <= 0
            || options.WindowSeconds > 3600)
        {
            return ValidateOptionsResult.Fail("RateLimiting permit limits and window must be positive, and the window must be at most 3600 seconds.");
        }

        return ValidateOptionsResult.Success;
    }
}

public sealed class MatchingOptions
{
    public const string SectionName = "Matching";

    public int ScoreThreshold { get; set; } = 50;
}

public sealed class MatchingOptionsValidator : IValidateOptions<MatchingOptions>
{
    public ValidateOptionsResult Validate(string? name, MatchingOptions options)
    {
        if (options.ScoreThreshold < 1 || options.ScoreThreshold > 100)
        {
            return ValidateOptionsResult.Fail("Matching:ScoreThreshold must be between 1 and 100.");
        }

        return ValidateOptionsResult.Success;
    }
}
