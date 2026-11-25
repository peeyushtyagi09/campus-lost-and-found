using LostAndFound.Api.Entities;

namespace LostAndFound.Api.Services;

public sealed record IssuedToken(string AccessToken, DateTime ExpiresAt);

public interface IJwtTokenService
{
    IssuedToken IssueToken(User user);
}
