using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class Disposal
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Public)]
    public ObjectId FoundReportId { get; set; }

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Public)]
    public DisposalMethod Method { get; set; }

    [FieldVisibility(Visibility.Private)]
    public ObjectId ApprovedBy { get; set; }

    [FieldVisibility(Visibility.Private)]
    public string Notes { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public DateTime DisposedAt { get; set; }
}
