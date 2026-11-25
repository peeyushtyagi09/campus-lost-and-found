using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class ItemMatch
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Public)]
    public ObjectId LostReportId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public ObjectId FoundReportId { get; set; }

    [FieldVisibility(Visibility.Private)]
    public ObjectId LostReporterId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public int Score { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string Explanation { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public MatchStatus Status { get; set; } = MatchStatus.Suggested;

    [FieldVisibility(Visibility.Public)]
    public DateTime CreatedAt { get; set; }

    [FieldVisibility(Visibility.Private)]
    public DateTime UpdatedAt { get; set; }
}
