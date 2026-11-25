namespace LostAndFound.Api.Dtos.Auth;

public sealed record RegisterRequest(string DisplayName, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserDto(string Id, string DisplayName, string Email, string Role);

public sealed record RegisterResponse(UserDto User);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, UserDto User);
