using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace net_ten_ecommerce.Models;

public class Order
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("items")]
    public List<OrderItem> Items { get; set; } = new();

    [BsonElement("shippingAddress")]
    public Address ShippingAddress { get; set; } = new();

    [BsonElement("billingAddress")]
    public Address BillingAddress { get; set; } = new();

    [BsonElement("status")]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [BsonElement("paymentMethod")]
    public PaymentMethod PaymentMethod { get; set; }

    [BsonElement("paymentStatus")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    [BsonElement("subtotal")]
    public decimal Subtotal { get; set; }

    [BsonElement("discount")]
    public decimal Discount { get; set; }

    [BsonElement("tax")]
    public decimal Tax { get; set; }

    [BsonElement("shippingCost")]
    public decimal ShippingCost { get; set; }

    [BsonElement("total")]
    public decimal Total { get; set; }

    [BsonElement("couponCode")]
    public string? CouponCode { get; set; }

    [BsonElement("trackingNumber")]
    public string? TrackingNumber { get; set; }

    [BsonElement("cargoCompany")]
    public string? CargoCompany { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("cancelReason")]
    public string? CancelReason { get; set; }

    [BsonElement("statusHistory")]
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("paidAt")]
    public DateTime? PaidAt { get; set; }

    [BsonElement("shippedAt")]
    public DateTime? ShippedAt { get; set; }

    [BsonElement("deliveredAt")]
    public DateTime? DeliveredAt { get; set; }

    [BsonElement("cancelledAt")]
    public DateTime? CancelledAt { get; set; }
}

public class OrderItem
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

public class Address
{
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("addressLine1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [BsonElement("addressLine2")]
    public string? AddressLine2 { get; set; }

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    [BsonElement("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = "Türkiye";
}

public class OrderStatusHistory
{
    [BsonElement("status")]
    public OrderStatus Status { get; set; }

    [BsonElement("note")]
    public string? Note { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("createdBy")]
    public string? CreatedBy { get; set; }
}

public enum OrderStatus
{
    Pending,           // Beklemede
    Confirmed,         // Onaylandı
    Processing,        // Hazırlanıyor
    Shipped,           // Kargoya verildi
    Delivered,         // Teslim edildi
    Cancelled,         // İptal edildi
    Returned,          // İade edildi
    Refunded           // Para iadesi yapıldı
}

public enum PaymentMethod
{
    CreditCard,        // Kredi Kartı
    BankTransfer,      // Havale/EFT
    CashOnDelivery     // Kapıda Ödeme
}

public enum PaymentStatus
{
    Pending,           // Beklemede
    Paid,              // Ödendi
    Failed,            // Başarısız
    Refunded           // İade edildi
}