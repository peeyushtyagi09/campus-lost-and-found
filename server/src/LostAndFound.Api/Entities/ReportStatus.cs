using System.Text.Json.Serialization;

namespace LostAndFound.Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReportStatus
{
    Open = 0,
    Matched = 1,
    Claimed = 2,
    Returned = 3,
    Closed = 4
}
