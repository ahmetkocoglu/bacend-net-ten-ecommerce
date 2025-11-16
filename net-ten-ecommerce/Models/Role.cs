using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace net_ten_ecommerce.Models;

public class Role
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("permissions")]
    public List<string> Permissions { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserRole
{
    [BsonElement("roleId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string RoleId { get; set; } = string.Empty;

    [BsonElement("roleName")]
    public string RoleName { get; set; } = string.Empty;

    [BsonElement("assignedAt")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

// Sabit roller
public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
    public const string Vendor = "Vendor";
    public const string Support = "Support";
}

// İzinler
public static class Permissions
{
    // Ürün yönetimi
    public const string ProductCreate = "product.create";
    public const string ProductEdit = "product.edit";
    public const string ProductDelete = "product.delete";
    public const string ProductView = "product.view";

    // Sipariş yönetimi
    public const string OrderView = "order.view";
    public const string OrderEdit = "order.edit";
    public const string OrderCancel = "order.cancel";
    public const string OrderRefund = "order.refund";

    // Kullanıcı yönetimi
    public const string UserCreate = "user.create";
    public const string UserEdit = "user.edit";
    public const string UserDelete = "user.delete";
    public const string UserView = "user.view";

    // Kargo yönetimi
    public const string CargoCreate = "cargo.create";
    public const string CargoCancel = "cargo.cancel";
    public const string CargoView = "cargo.view";

    // Kupon yönetimi
    public const string CouponCreate = "coupon.create";
    public const string CouponEdit = "coupon.edit";
    public const string CouponDelete = "coupon.delete";

    // Raporlama
    public const string ReportsView = "reports.view";
    public const string ReportsExport = "reports.export";
}