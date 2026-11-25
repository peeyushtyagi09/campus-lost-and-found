using LostAndFound.Api.Dtos.Auth;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public sealed class AuthService : IAuthService
{
    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _users;
    private readonly IEncryptionService _encryption;
    private readonly IEmailHasher _emailHasher;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository users,
        IEncryptionService encryption,
        IEmailHasher emailHasher,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _users = users;
        _encryption = encryption;
        _emailHasher = emailHasher;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<ServiceResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = _emailHasher.Normalize(request.Email);
        var emailHash = _emailHasher.Hash(normalizedEmail);

        var existing = await _users.GetByEmailHashAsync(emailHash, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Registration rejected due to duplicate email hash.");
            return ServiceResult<RegisterResponse>.Failure(
                StatusCodes.Status409Conflict,
                "Registration could not be completed.");
        }

        var user = new User
        {
            Id = ObjectId.GenerateNewId(),
            DisplayName = request.DisplayName.Trim(),
            Email = _encryption.Encrypt(normalizedEmail),
            EmailHash = emailHash,
            Role = UserRole.User,
            FailedLoginCount = 0,
            LockoutEnd = null,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        try
        {
            await _users.CreateAsync(user, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            _logger.LogInformation("Registration rejected due to concurrent duplicate email hash.");
            return ServiceResult<RegisterResponse>.Failure(
                StatusCodes.Status409Conflict,
                "Registration could not be completed.");
        }

        _logger.LogInformation("Registered new user {UserId}.", user.Id);
        var dto = new UserDto(user.Id.ToString(), user.DisplayName, normalizedEmail, user.Role.ToString());
        return ServiceResult<RegisterResponse>.Success(new RegisterResponse(dto), StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = _emailHasher.Normalize(request.Email);
        var emailHash = _emailHasher.Hash(normalizedEmail);

        var user = await _users.GetByEmailHashAsync(emailHash, cancellationToken);
        if (user is null)
        {
            return ServiceResult<LoginResponse>.Failure(
                StatusCodes.Status401Unauthorized,
                "Invalid credentials.");
        }

        var now = DateTime.UtcNow;
        if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > now)
        {
            _logger.LogWarning("Blocked login attempt for locked user {UserId}.", user.Id);
            return ServiceResult<LoginResponse>.Failure(
                StatusCodes.Status423Locked,
                "Account is temporarily locked due to repeated failed attempts.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            var nextFailedCount = user.FailedLoginCount + 1;
            DateTime? nextLockoutEnd = nextFailedCount >= MaxFailedLoginAttempts
                ? now.Add(LockoutDuration)
                : null;

            await _users.UpdateLoginStateAsync(
                user.Id,
                nextFailedCount,
                nextLockoutEnd,
                upgradedPasswordHash: null,
                cancellationToken);

            _logger.LogWarning("Failed login attempt {Attempt} for user {UserId}.", nextFailedCount, user.Id);
            return ServiceResult<LoginResponse>.Failure(
                StatusCodes.Status401Unauthorized,
                "Invalid credentials.");
        }

        string? upgradedHash = verification == PasswordVerificationResult.SuccessRehashNeeded
            ? _passwordHasher.HashPassword(user, request.Password)
            : null;

        if (user.FailedLoginCount != 0 || user.LockoutEnd is not null || upgradedHash is not null)
        {
            await _users.UpdateLoginStateAsync(
                user.Id,
                failedLoginCount: 0,
                lockoutEnd: null,
                upgradedPasswordHash: upgradedHash,
                cancellationToken);
        }

        var token = _jwtTokenService.IssueToken(user);
        var email = _encryption.Decrypt(user.Email);
        var dto = new UserDto(user.Id.ToString(), user.DisplayName, email, user.Role.ToString());

        _logger.LogInformation("Authenticated user {UserId}.", user.Id);
        return ServiceResult<LoginResponse>.Success(new LoginResponse(token.AccessToken, token.ExpiresAt, dto));
    }

    public async Task<ServiceResult<UserDto>> GetMeAsync(string userId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(userId, out var objectId))
        {
            return ServiceResult<UserDto>.Failure(StatusCodes.Status401Unauthorized, "Authentication required.");
        }

        var user = await _users.GetByIdAsync(objectId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<UserDto>.Failure(StatusCodes.Status401Unauthorized, "Authentication required.");
        }

        var email = _encryption.Decrypt(user.Email);
        var dto = new UserDto(user.Id.ToString(), user.DisplayName, email, user.Role.ToString());
        return ServiceResult<UserDto>.Success(dto);
    }
}
