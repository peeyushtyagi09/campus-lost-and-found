using System.Text.Json.Serialization;

namespace LostAndFound.Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    User = 0,
    Staff = 1,
    Admin = 2
}
