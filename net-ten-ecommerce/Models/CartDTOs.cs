namespace net_ten_ecommerce.Models;

public class AddToCartRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string? Variant { get; set; }
}

public class UpdateCartItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? Variant { get; set; }
}

public class ApplyCouponRequest
{
    public string CouponCode { get; set; } = string.Empty;
}

public class CartResponse
{
    public string? Id { get; set; }
    public List<CartItemResponse> Items { get; set; } = new();
    public string? CouponCode { get; set; }
    public decimal Discount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CartItemResponse
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal FinalPrice { get; set; }
    public int Quantity { get; set; }
    public string? Variant { get; set; }
    public decimal Subtotal { get; set; }
    public bool InStock { get; set; }
    public int AvailableStock { get; set; }
}

public class CreateCouponRequest
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MinPurchaseAmount { get; set; } = 0;
    public decimal? MaxDiscountAmount { get; set; }
    public int? UsageLimit { get; set; }
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
    public DateTime ValidUntil { get; set; }
}