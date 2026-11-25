using System.Text.Json.Serialization;

namespace LostAndFound.Api.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ItemCategory
{
    Phone = 0,
    Laptop = 1,
    Wallet = 2,
    IdCard = 3,
    Keys = 4,
    Bag = 5,
    Clothing = 6,
    Books = 7,
    Other = 8
}
