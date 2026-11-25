using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LostAndFound.Api.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
        var key = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public IssuedToken IssueToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_options.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: _signingCredentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new IssuedToken(accessToken, expiresAt);
    }
}
