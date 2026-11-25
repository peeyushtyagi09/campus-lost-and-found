using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class AuditEvent
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string Action { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Private)]
    public ObjectId ActorId { get; set; }

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public UserRole ActorRole { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string? TargetId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string Summary { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public DateTime Timestamp { get; set; }
}
