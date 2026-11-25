using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class Notification
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Private)]
    public ObjectId RecipientUserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public NotificationType Type { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string Message { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public string? RelatedEntityId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public bool IsRead { get; set; }

    [FieldVisibility(Visibility.Public)]
    public DateTime CreatedAt { get; set; }
}
