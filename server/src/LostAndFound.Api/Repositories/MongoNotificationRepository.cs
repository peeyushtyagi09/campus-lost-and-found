using LostAndFound.Api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoNotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<Notification> _notifications;

    public MongoNotificationRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _notifications = context.Notifications;
    }

    public async Task CreateAsync(Notification notification, CancellationToken cancellationToken)
    {
        await _notifications.InsertOneAsync(notification, cancellationToken: cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken)
    {
        return await _notifications.Find(n => n.Id == id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetByRecipientAsync(
        ObjectId recipientId,
        CancellationToken cancellationToken)
    {
        return await _notifications
            .Find(n => n.RecipientUserId == recipientId)
            .SortBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt)
            .Limit(50)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(ObjectId id, CancellationToken cancellationToken)
    {
        var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
        await _notifications.UpdateOneAsync(n => n.Id == id, update, cancellationToken: cancellationToken);
    }
}
