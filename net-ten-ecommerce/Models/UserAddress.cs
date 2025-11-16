using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace net_ten_ecommerce.Models;

public class UserAddress
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("address")]
    public Address Address { get; set; } = new();

    [BsonElement("label")]
    public string Label { get; set; } = "Ev";

    [BsonElement("addressType")]
    public AddressType AddressType { get; set; } = AddressType.Home;

    [BsonElement("isDefaultShipping")]
    public bool IsDefaultShipping { get; set; }

    [BsonElement("isDefaultBilling")]
    public bool IsDefaultBilling { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum AddressType
{
    Home,
    Work,
    Other
}