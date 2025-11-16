using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace net_ten_ecommerce.Models;

public class Cart
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UserId { get; set; }

    [BsonElement("sessionId")]
    public string? SessionId { get; set; }

    [BsonElement("items")]
    public List<CartItem> Items { get; set; } = new();

    [BsonElement("couponCode")]
    public string? CouponCode { get; set; }

    [BsonElement("discount")]
    public decimal Discount { get; set; } = 0;

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; } = 0;

    [BsonElement("tax")]
    public decimal Tax { get; set; } = 0;

    [BsonElement("shippingCost")]
    public decimal ShippingCost { get; set; } = 0;

    [BsonElement("total")]
    public decimal Total { get; set; } = 0;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("expiresAt")]
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
}

public class CartItem
{
    [BsonElement("productId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string ProductId { get; set; } = string.Empty;

    [BsonElement("productName")]
    public string ProductName { get; set; } = string.Empty;

    [BsonElement("productImage")]
    public string ProductImage { get; set; } = string.Empty;

    [BsonElement("sku")]
    public string SKU { get; set; } = string.Empty;

    [BsonElement("price")]
    public decimal Price { get; set; }

    [BsonElement("discountPrice")]
    public decimal? DiscountPrice { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("variant")]
    public string? Variant { get; set; }

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }
}

public class Coupon
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("code")]
    public string Code { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("discountType")]
    public DiscountType DiscountType { get; set; }

    [BsonElement("discountValue")]
    public decimal DiscountValue { get; set; }

    [BsonElement("minPurchaseAmount")]
    public decimal MinPurchaseAmount { get; set; } = 0;

    [BsonElement("maxDiscountAmount")]
    public decimal? MaxDiscountAmount { get; set; }

    [BsonElement("usageLimit")]
    public int? UsageLimit { get; set; }

    [BsonElement("usageCount")]
    public int UsageCount { get; set; } = 0;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("validFrom")]
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

    [BsonElement("validUntil")]
    public DateTime ValidUntil { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum DiscountType
{
    Percentage,
    FixedAmount
}