using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class Claim
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Private)]
    public ObjectId ClaimantId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public ObjectId FoundReportId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public ObjectId? LostReportId { get; set; }

    [FieldVisibility(Visibility.Private)]
    public string VerificationAnswers { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

    [FieldVisibility(Visibility.Private)]
    public ObjectId? ReviewedBy { get; set; }

    [FieldVisibility(Visibility.Private)]
    public string? ReviewNotes { get; set; }

    [FieldVisibility(Visibility.Public)]
    public DateTime CreatedAt { get; set; }

    [FieldVisibility(Visibility.Private)]
    public DateTime UpdatedAt { get; set; }
}
