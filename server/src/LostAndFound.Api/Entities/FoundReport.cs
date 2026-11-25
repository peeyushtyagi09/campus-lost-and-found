using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class FoundReport
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Private)]
    public ObjectId FinderId { get; set; }

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
    public DateTime DateFound { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string HeldAt { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public string? PhotoFileName { get; set; }

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public ReportStatus Status { get; set; } = ReportStatus.Open;

    [FieldVisibility(Visibility.Private)]
    public string StorageNotes { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Private)]
    public string FinderContact { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Private)]
    public string VerificationDetails { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public DateTime CreatedAt { get; set; }

    [FieldVisibility(Visibility.Private)]
    public DateTime UpdatedAt { get; set; }

    [FieldVisibility(Visibility.Public)]
    public bool EligibleForDisposal { get; set; }

    [FieldVisibility(Visibility.Private)]
    public DateTime? DisposalFlaggedAt { get; set; }
}
