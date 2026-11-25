using System.Text.Json.Serialization;

namespace LostAndFound.Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClaimStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Withdrawn = 3
}
