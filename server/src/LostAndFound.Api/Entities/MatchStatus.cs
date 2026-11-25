using System.Text.Json.Serialization;

namespace LostAndFound.Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MatchStatus
{
    Suggested = 0,
    Confirmed = 1,
    Dismissed = 2
}
