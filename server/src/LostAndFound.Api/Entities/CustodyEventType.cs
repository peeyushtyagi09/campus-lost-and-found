using System.Text.Json.Serialization;

namespace LostAndFound.Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CustodyEventType
{
    Received = 0,
    Stored = 1,
    ClaimSubmitted = 2,
    ClaimApproved = 3,
    HandedOver = 4,
    Rejected = 5,
    Disposed = 6
}
