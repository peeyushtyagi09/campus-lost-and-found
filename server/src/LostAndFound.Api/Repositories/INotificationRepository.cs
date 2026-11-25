using LostAndFound.Api.Entities;
using MongoDB.Bson;

namespace LostAndFound.Api.Repositories;

public interface INotificationRepository
{
    Task CreateAsync(Notification notification, CancellationToken cancellationToken);

    Task<Notification?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Notification>> GetByRecipientAsync(ObjectId recipientId, CancellationToken cancellationToken);

    Task MarkAsReadAsync(ObjectId id, CancellationToken cancellationToken);
}
