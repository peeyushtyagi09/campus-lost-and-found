using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class Handover
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Public)]
    public ObjectId ClaimId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public ObjectId FoundReportId { get; set; }

    [FieldVisibility(Visibility.Private)]
    public ObjectId StaffId { get; set; }

    [FieldVisibility(Visibility.Public)]
    public bool WitnessConfirmed { get; set; }

    [FieldVisibility(Visibility.Public)]
    public bool IdChecked { get; set; }

    [FieldVisibility(Visibility.Private)]
    public string Notes { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Public)]
    public DateTime CompletedAt { get; set; }
}
