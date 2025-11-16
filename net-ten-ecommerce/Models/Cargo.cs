using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace net_ten_ecommerce.Models;

public class CargoShipment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("orderId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OrderId { get; set; } = string.Empty;

    [BsonElement("cargoCompany")]
    public CargoCompany CargoCompany { get; set; }

    [BsonElement("trackingNumber")]
    public string TrackingNumber { get; set; } = string.Empty;

    [BsonElement("barcode")]
    public string? Barcode { get; set; }

    [BsonElement("status")]
    public CargoStatus Status { get; set; } = CargoStatus.Created;

    [BsonElement("senderInfo")]
    public CargoContact SenderInfo { get; set; } = new();

    [BsonElement("receiverInfo")]
    public CargoContact ReceiverInfo { get; set; } = new();

    [BsonElement("packageInfo")]
    public PackageInfo PackageInfo { get; set; } = new();

    [BsonElement("estimatedDeliveryDate")]
    public DateTime? EstimatedDeliveryDate { get; set; }

    [BsonElement("actualDeliveryDate")]
    public DateTime? ActualDeliveryDate { get; set; }

    [BsonElement("trackingHistory")]
    public List<CargoTrackingEvent> TrackingHistory { get; set; } = new();

    [BsonElement("cost")]
    public decimal Cost { get; set; }

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class CargoContact
{
    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("district")]
    public string District { get; set; } = string.Empty;

    [BsonElement("postalCode")]
    public string PostalCode { get; set; } = string.Empty;
}

public class PackageInfo
{
    [BsonElement("weight")]
    public decimal Weight { get; set; } // kg

    [BsonElement("desi")]
    public decimal Desi { get; set; }

    [BsonElement("width")]
    public decimal Width { get; set; } // cm

    [BsonElement("height")]
    public decimal Height { get; set; } // cm

    [BsonElement("length")]
    public decimal Length { get; set; } // cm

    [BsonElement("packageCount")]
    public int PackageCount { get; set; } = 1;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("value")]
    public decimal Value { get; set; } // Kargo değeri
}

public class CargoTrackingEvent
{
    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("location")]
    public string? Location { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("isDelivered")]
    public bool IsDelivered { get; set; }
}

public enum CargoCompany
{
    ArasKargo,
    MNGKargo,
    YurticiKargo,
    PTT,
    SuratKargo,
    UPS
}

public enum CargoStatus
{
    Created,           // Oluşturuldu
    PickedUp,          // Alındı
    InTransit,         // Yolda
    InBranch,          // Şubede
    OutForDelivery,    // Dağıtımda
    Delivered,         // Teslim edildi
    FailedDelivery,    // Teslim başarısız
    Returned,          // İade
    Cancelled          // İptal
}