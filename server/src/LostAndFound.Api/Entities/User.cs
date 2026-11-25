using LostAndFound.Api.Privacy;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace LostAndFound.Api.Entities;

public sealed class User
{
    [BsonId]
    [FieldVisibility(Visibility.Public)]
    public ObjectId Id { get; set; }

    [FieldVisibility(Visibility.Public)]
    public string DisplayName { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Private)]
    public string Email { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Private)]
    public string EmailHash { get; set; } = string.Empty;

    [FieldVisibility(Visibility.Private)]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    [FieldVisibility(Visibility.Private)]
    public UserRole Role { get; set; } = UserRole.User;

    [FieldVisibility(Visibility.Private)]
    public int FailedLoginCount { get; set; }

    [FieldVisibility(Visibility.Private)]
    public DateTime? LockoutEnd { get; set; }

    [FieldVisibility(Visibility.Private)]
    public DateTime CreatedAt { get; set; }
}
