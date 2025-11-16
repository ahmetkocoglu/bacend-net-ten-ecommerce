namespace net_ten_ecommerce.Models;

public class CreateOrderRequest
{
    public Address ShippingAddress { get; set; } = new();
    public Address BillingAddress { get; set; } = new();
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public bool UseSameAddressForBilling { get; set; } = true;
}

public class OrderListRequest
{
    public string? UserId { get; set; }
    public OrderStatus? Status { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class OrderListResponse
{
    public List<OrderSummary> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class OrderSummary
{
    public string Id { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public string? TrackingNumber { get; set; }
    public string? CargoCompany { get; set; }
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class OrderStats
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int ProcessingOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
}