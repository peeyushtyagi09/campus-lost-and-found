using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class CustodyEvent
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Public)]
    public ObjectId ReportId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public long SequenceNumber { get; set; }

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public CustodyEventType EventType { get; set; }

    [FieldVisibility(Visibility.Private)]
    public ObjectId ActorId { get; set; }

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public UserRole ActorRole { get; set; }

    [FieldVisibility(Visibility.Public)]
    public DateTime Timestamp { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string Notes { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public string PreviousEventHash { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public string EventHash { get; set; } = string.Empty;
}
