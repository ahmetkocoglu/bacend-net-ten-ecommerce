using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace net_ten_ecommerce.Models;

public class Product
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("shortDescription")]
    public string ShortDescription { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("discountPrice")]
    public decimal? DiscountPrice { get; set; }

    [BsonElement("categoryId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string CategoryId { get; set; } = string.Empty;

    [BsonElement("brandId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? BrandId { get; set; }

    [BsonElement("sku")]
    public string SKU { get; set; } = string.Empty;

    [BsonElement("stock")]
    public int Stock { get; set; }

    [BsonElement("images")]
    public List<string> Images { get; set; } = new();

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("specifications")]
    public Dictionary<string, string> Specifications { get; set; } = new();

    [BsonElement("variants")]
    public List<ProductVariant> Variants { get; set; } = new();

    [BsonElement("rating")]
    public double Rating { get; set; } = 0;

    [BsonElement("reviewCount")]
    public int ReviewCount { get; set; } = 0;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("isFeatured")]
    public bool IsFeatured { get; set; } = false;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ProductVariant
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("value")]
    public string Value { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal? Price { get; set; }

    [BsonElement("stock")]
    public int Stock { get; set; }

    [BsonElement("sku")]
    public string SKU { get; set; } = string.Empty;
}

public class Category
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("parentId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ParentId { get; set; }

    [BsonElement("image")]
    public string? Image { get; set; }

    [BsonElement("order")]
    public int Order { get; set; } = 0;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Brand
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("logo")]
    public string? Logo { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}