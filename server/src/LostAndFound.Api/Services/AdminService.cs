using LostAndFound.Api.Dtos.Admin;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public interface IAdminService
{
    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken cancellationToken);

    Task<ServiceResult<AdminUserDto>> CreateStaffAsync(
        string adminUserId,
        CreateStaffRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AdminUserDto>> UpdateRoleAsync(
        string adminUserId,
        string targetUserId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken);
}

public sealed class AdminService : IAdminService
{
    private readonly IUserRepository _users;
    private readonly IEncryptionService _encryption;
    private readonly IEmailHasher _emailHasher;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAuditService _audit;

    public AdminService(
        IUserRepository users,
        IEncryptionService encryption,
        IEmailHasher emailHasher,
        IPasswordHasher<User> passwordHasher,
        IAuditService audit)
    {
        _users = users;
        _encryption = encryption;
        _emailHasher = emailHasher;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _users.ListAllAsync(cancellationToken);
        return users.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<AdminUserDto>> CreateStaffAsync(
        string adminUserId,
        CreateStaffRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(adminUserId, out var actorId))
        {
            return ServiceResult<AdminUserDto>.Failure(StatusCodes.Status401Unauthorized, "Authentication required.");
        }

        var normalized = _emailHasher.Normalize(request.Email);
        var emailHash = _emailHasher.Hash(normalized);
        if (await _users.GetByEmailHashAsync(emailHash, cancellationToken) is not null)
        {
            return ServiceResult<AdminUserDto>.Failure(
                StatusCodes.Status409Conflict,
                "Account creation could not be completed.");
        }

        var staff = new User
        {
            Id = ObjectId.GenerateNewId(),
            DisplayName = request.DisplayName.Trim(),
            Email = _encryption.Encrypt(normalized),
            EmailHash = emailHash,
            Role = UserRole.Staff,
            CreatedAt = DateTime.UtcNow
        };
        staff.PasswordHash = _passwordHasher.HashPassword(staff, request.Password);

        try
        {
            await _users.CreateAsync(staff, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            return ServiceResult<AdminUserDto>.Failure(
                StatusCodes.Status409Conflict,
                "Account creation could not be completed.");
        }

        await _audit.RecordAsync(
            "StaffAccountCreated",
            actorId,
            UserRole.Admin,
            staff.Id.ToString(),
            "Created staff account.",
            cancellationToken);

        return ServiceResult<AdminUserDto>.Success(ToDto(staff), StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<AdminUserDto>> UpdateRoleAsync(
        string adminUserId,
        string targetUserId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(adminUserId, out var actorId) || !ObjectId.TryParse(targetUserId, out var targetId))
        {
            return ServiceResult<AdminUserDto>.Failure(StatusCodes.Status404NotFound, "User not found.");
        }

        var user = await _users.GetByIdAsync(targetId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<AdminUserDto>.Failure(StatusCodes.Status404NotFound, "User not found.");
        }

        if (user.Role == UserRole.Admin)
        {
            return ServiceResult<AdminUserDto>.Failure(
                StatusCodes.Status409Conflict,
                "Admin account roles cannot be modified through this endpoint.");
        }

        await _users.UpdateRoleAsync(targetId, request.Role, cancellationToken);
        user.Role = request.Role;

        await _audit.RecordAsync(
            "UserRoleUpdated",
            actorId,
            UserRole.Admin,
            targetId.ToString(),
            $"Updated user role to {request.Role}.",
            cancellationToken);

        return ServiceResult<AdminUserDto>.Success(ToDto(user));
    }

    private static AdminUserDto ToDto(User user)
        => new(user.Id.ToString(), user.DisplayName, user.Role, user.CreatedAt);
}
