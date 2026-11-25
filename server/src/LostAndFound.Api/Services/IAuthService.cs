using LostAndFound.Api.Dtos.Auth;

namespace LostAndFound.Api.Services;

public interface IAuthService
{
    Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<UserDto>> GetMeAsync(string userId, CancellationToken cancellationToken);
}
