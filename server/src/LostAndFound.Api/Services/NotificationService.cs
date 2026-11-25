using LostAndFound.Api.Dtos.Admin;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public interface INotificationService
{
    Task NotifyUserAsync(
        ObjectId recipientUserId,
        NotificationType type,
        string message,
        string? relatedEntityId,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<NotificationDto>>> GetMineAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<ServiceResult<NotificationDto>> MarkReadAsync(
        string notificationId,
        string userId,
        CancellationToken cancellationToken);
}

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly IEncryptionService _encryption;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationRepository notifications,
        IUserRepository users,
        IEncryptionService encryption,
        IEmailSender emailSender,
        ILogger<NotificationService> logger)
    {
        _notifications = notifications;
        _users = users;
        _encryption = encryption;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task NotifyUserAsync(
        ObjectId recipientUserId,
        NotificationType type,
        string message,
        string? relatedEntityId,
        CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            Id = ObjectId.GenerateNewId(),
            RecipientUserId = recipientUserId,
            Type = type,
            Message = message,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notifications.CreateAsync(notification, cancellationToken);

        var user = await _users.GetByIdAsync(recipientUserId, cancellationToken);
        if (user is not null)
        {
            try
            {
                var email = _encryption.Decrypt(user.Email);
                await _emailSender.SendGenericNoticeAsync(
                    email,
                    recipientUserId.ToString(),
                    "Campus Lost & Found update",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispatch email notification for user {UserId}.", recipientUserId);
            }
        }
    }

    public async Task<ServiceResult<IReadOnlyList<NotificationDto>>> GetMineAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(userId, out var recipientId))
        {
            return ServiceResult<IReadOnlyList<NotificationDto>>.Failure(
                StatusCodes.Status401Unauthorized,
                "Authentication required.");
        }

        var items = await _notifications.GetByRecipientAsync(recipientId, cancellationToken);
        return ServiceResult<IReadOnlyList<NotificationDto>>.Success(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<NotificationDto>> MarkReadAsync(
        string notificationId,
        string userId,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(notificationId, out var id) || !ObjectId.TryParse(userId, out var callerId))
        {
            return ServiceResult<NotificationDto>.Failure(StatusCodes.Status404NotFound, "Notification not found.");
        }

        var notification = await _notifications.GetByIdAsync(id, cancellationToken);
        if (notification is null || notification.RecipientUserId != callerId)
        {
            return ServiceResult<NotificationDto>.Failure(StatusCodes.Status404NotFound, "Notification not found.");
        }

        if (!notification.IsRead)
        {
            await _notifications.MarkAsReadAsync(id, cancellationToken);
            notification.IsRead = true;
        }

        return ServiceResult<NotificationDto>.Success(ToDto(notification));
    }

    private static NotificationDto ToDto(Notification notification)
        => new(
            notification.Id.ToString(),
            notification.Type,
            notification.Message,
            notification.RelatedEntityId,
            notification.IsRead,
            notification.CreatedAt);
}
