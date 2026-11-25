using System.Text.Json.Serialization;

namespace LostAndFound.Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisposalMethod
{
    Donate = 0,
    Recycle = 1,
    Destroy = 2
}
