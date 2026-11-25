using System.Text.Json.Serialization;

namespace LostAndFound.Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NotificationType
{
    MatchFound = 0,
    ClaimStatusChanged = 1,
    HandoverCompleted = 2
}
