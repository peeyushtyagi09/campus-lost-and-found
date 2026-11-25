using LostAndFound.Api.Entities;

namespace LostAndFound.Api.Dtos.Admin;

public sealed record ApproveDisposalRequest(DisposalMethod Method, string Notes);

public sealed record DisposalDto(
    string Id,
    string FoundReportId,
    DisposalMethod Method,
    DateTime DisposedAt);

public sealed record NotificationDto(
    string Id,
    NotificationType Type,
    string Message,
    string? RelatedEntityId,
    bool IsRead,
    DateTime CreatedAt);

public sealed record AdminUserDto(
    string Id,
    string DisplayName,
    UserRole Role,
    DateTime CreatedAt);

public sealed record CreateStaffRequest(string DisplayName, string Email, string Password);

public sealed record UpdateUserRoleRequest(UserRole Role);

public sealed record AuditEventDto(
    string Id,
    string Action,
    UserRole ActorRole,
    string? TargetId,
    string Summary,
    DateTime Timestamp);
