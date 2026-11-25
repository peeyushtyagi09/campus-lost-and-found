using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class LostReport
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Private)]
    public ObjectId ReporterId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string Title { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public ItemCategory Category { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string PublicDescription { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public string CampusArea { get; set; } = string.Empty;

    [BsonDateTimeOptions(DateOnly = true)]
    [FieldVisibility(Visibility.Public)]
    public DateTime DateLost { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string? PhotoFileName { get; set; }

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public ReportStatus Status { get; set; } = ReportStatus.Open;

    [FieldVisibility(Visibility.Private)]
    public string PrivateDetails { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Private)]
    public string ContactPhone { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Private)]
    public string ContactEmail { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public DateTime CreatedAt { get; set; }

    [FieldVisibility(Visibility.Private)]
    public DateTime UpdatedAt { get; set; }
}
