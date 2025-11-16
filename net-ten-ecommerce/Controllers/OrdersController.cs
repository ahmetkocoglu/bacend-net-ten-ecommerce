using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;
using System.Security.Claims;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMongoCollection<Order> _orders;
    private readonly IMongoCollection<Cart> _carts;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Coupon> _coupons;
    private readonly IMongoCollection<User> _users;

    public OrdersController(IMongoDatabase database)
    {
        _orders = database.GetCollection<Order>("Orders");
        _carts = database.GetCollection<Cart>("Carts");
        _products = database.GetCollection<Product>("Products");
        _coupons = database.GetCollection<Coupon>("Coupons");
        _users = database.GetCollection<User>("Users");
    }

    [HttpGet]
    public async Task<ActionResult<OrderListResponse>> GetOrders([FromQuery] OrderListRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");

        var filterBuilder = Builders<Order>.Filter;
        var filters = new List<FilterDefinition<Order>>();

        // Admin değilse sadece kendi siparişlerini görsün
        if (!isAdmin)
        {
            filters.Add(filterBuilder.Eq(o => o.UserId, userId));
        }
        else if (!string.IsNullOrEmpty(request.UserId))
        {
            filters.Add(filterBuilder.Eq(o => o.UserId, request.UserId));
        }

        // Durum filtresi
        if (request.Status.HasValue)
            filters.Add(filterBuilder.Eq(o => o.Status, request.Status.Value));

        // Ödeme durumu filtresi
        if (request.PaymentStatus.HasValue)
            filters.Add(filterBuilder.Eq(o => o.PaymentStatus, request.PaymentStatus.Value));

        // Tarih aralığı
        if (request.StartDate.HasValue)
            filters.Add(filterBuilder.Gte(o => o.CreatedAt, request.StartDate.Value));

        if (request.EndDate.HasValue)
            filters.Add(filterBuilder.Lte(o => o.CreatedAt, request.EndDate.Value));

        // Arama
        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchFilter = filterBuilder.Or(
                filterBuilder.Regex(o => o.OrderNumber, new MongoDB.Bson.BsonRegularExpression(request.Search, "i")),
                filterBuilder.Regex(o => o.ShippingAddress.FullName, new MongoDB.Bson.BsonRegularExpression(request.Search, "i"))
            );
            filters.Add(searchFilter);
        }

        var finalFilter = filters.Any() ? filterBuilder.And(filters) : filterBuilder.Empty;

        var totalCount = await _orders.CountDocumentsAsync(finalFilter);

        var orders = await _orders.Find(finalFilter)
            .SortByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync();

        var orderSummaries = orders.Select(o => new OrderSummary
        {
            Id = o.Id!,
            OrderNumber = o.OrderNumber,
            ItemCount = o.Items.Sum(i => i.Quantity),
            Total = o.Total,
            Status = o.Status,
            PaymentStatus = o.PaymentStatus,
            CreatedAt = o.CreatedAt
        }).ToList();

        return Ok(new OrderListResponse
        {
            Orders = orderSummaries,
            TotalCount = (int)totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");

        var order = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();

        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        // Admin değilse sadece kendi siparişini görebilir
        if (!isAdmin && order.UserId != userId)
            return Forbid();

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Sepeti al
        var cart = await _carts.Find(c => c.UserId == userId).FirstOrDefaultAsync();
        if (cart == null || !cart.Items.Any())
            return BadRequest(new { message = "Sepetiniz boş." });

        // Stok kontrolü
        foreach (var item in cart.Items)
        {
            var product = await _products.Find(p => p.Id == item.ProductId).FirstOrDefaultAsync();
            if (product == null)
                return BadRequest(new { message = $"{item.ProductName} ürünü bulunamadı." });

            if (product.Stock < item.Quantity)
                return BadRequest(new { message = $"{item.ProductName} için yeterli stok yok." });
        }

        // Sipariş numarası oluştur
        var orderNumber = GenerateOrderNumber();

        // Sipariş oluştur
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = userId,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductImage = i.ProductImage,
                SKU = i.SKU,
                Price = i.Price,
                DiscountPrice = i.DiscountPrice,
                Quantity = i.Quantity,
                Variant = i.Variant,
                Subtotal = i.Subtotal
            }).ToList(),
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.UseSameAddressForBilling ? request.ShippingAddress : request.BillingAddress,
            PaymentMethod = request.PaymentMethod,
            Subtotal = cart.Subtotal,
            Discount = cart.Discount,
            Tax = cart.Tax,
            ShippingCost = cart.ShippingCost,
            Total = cart.Total,
            CouponCode = cart.CouponCode,
            Notes = request.Notes,
            Status = OrderStatus.Pending,
            PaymentStatus = request.PaymentMethod == PaymentMethod.CashOnDelivery 
                ? PaymentStatus.Pending 
                : PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Durum geçmişi ekle
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Pending,
            Note = "Sipariş oluşturuldu",
            CreatedAt = DateTime.UtcNow
        });

        // Siparişi kaydet
        await _orders.InsertOneAsync(order);

        // Stokları güncelle
        foreach (var item in cart.Items)
        {
            var update = Builders<Product>.Update
                .Inc(p => p.Stock, -item.Quantity)
                .Set(p => p.UpdatedAt, DateTime.UtcNow);
            
            await _products.UpdateOneAsync(p => p.Id == item.ProductId, update);
        }

        // Kupon kullanım sayısını artır
        if (!string.IsNullOrEmpty(cart.CouponCode))
        {
            var update = Builders<Coupon>.Update.Inc(c => c.UsageCount, 1);
            await _coupons.UpdateOneAsync(c => c.Code == cart.CouponCode, update);
        }

        // Sepeti temizle
        cart.Items.Clear();
        cart.CouponCode = null;
        cart.Discount = 0;
        cart.Subtotal = 0;
        cart.Tax = 0;
        cart.ShippingCost = 0;
        cart.Total = 0;
        await _carts.ReplaceOneAsync(c => c.Id == cart.Id, cart);

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<Order>> UpdateOrderStatus(
        string id, 
        [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        var updateBuilder = Builders<Order>.Update;
        var updates = new List<UpdateDefinition<Order>>();

        updates.Add(updateBuilder.Set(o => o.Status, request.Status));
        updates.Add(updateBuilder.Set(o => o.UpdatedAt, DateTime.UtcNow));

        // Durum geçmişine ekle
        var statusHistory = new OrderStatusHistory
        {
            Status = request.Status,
            Note = request.Note,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.FindFirst(ClaimTypes.Email)?.Value
        };
        updates.Add(updateBuilder.Push(o => o.StatusHistory, statusHistory));

        // Duruma göre tarih güncelle
        switch (request.Status)
        {
            case OrderStatus.Confirmed:
                updates.Add(updateBuilder.Set(o => o.PaymentStatus, PaymentStatus.Paid));
                updates.Add(updateBuilder.Set(o => o.PaidAt, DateTime.UtcNow));
                break;
            case OrderStatus.Shipped:
                updates.Add(updateBuilder.Set(o => o.ShippedAt, DateTime.UtcNow));
                if (!string.IsNullOrEmpty(request.TrackingNumber))
                    updates.Add(updateBuilder.Set(o => o.TrackingNumber, request.TrackingNumber));
                if (!string.IsNullOrEmpty(request.CargoCompany))
                    updates.Add(updateBuilder.Set(o => o.CargoCompany, request.CargoCompany));
                break;
            case OrderStatus.Delivered:
                updates.Add(updateBuilder.Set(o => o.DeliveredAt, DateTime.UtcNow));
                break;
            case OrderStatus.Cancelled:
                updates.Add(updateBuilder.Set(o => o.CancelledAt, DateTime.UtcNow));
                // Stokları geri ekle
                foreach (var item in order.Items)
                {
                    var stockUpdate = Builders<Product>.Update.Inc(p => p.Stock, item.Quantity);
                    await _products.UpdateOneAsync(p => p.Id == item.ProductId, stockUpdate);
                }
                break;
        }

        var combinedUpdate = updateBuilder.Combine(updates);
        await _orders.UpdateOneAsync(o => o.Id == id, combinedUpdate);

        var updatedOrder = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        return Ok(updatedOrder);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<Order>> CancelOrder(string id, [FromBody] CancelOrderRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("Admin");

        var order = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        // Admin değilse sadece kendi siparişini iptal edebilir
        if (!isAdmin && order.UserId != userId)
            return Forbid();

        // İptal edilebilir durumda mı kontrol et
        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
            return BadRequest(new { message = "Bu sipariş iptal edilemez." });

        var update = Builders<Order>.Update
            .Set(o => o.Status, OrderStatus.Cancelled)
            .Set(o => o.CancelReason, request.Reason)
            .Set(o => o.CancelledAt, DateTime.UtcNow)
            .Set(o => o.UpdatedAt, DateTime.UtcNow)
            .Push(o => o.StatusHistory, new OrderStatusHistory
            {
                Status = OrderStatus.Cancelled,
                Note = $"İptal nedeni: {request.Reason}",
                CreatedAt = DateTime.UtcNow
            });

        await _orders.UpdateOneAsync(o => o.Id == id, update);

        // Stokları geri ekle
        foreach (var item in order.Items)
        {
            var stockUpdate = Builders<Product>.Update.Inc(p => p.Stock, item.Quantity);
            await _products.UpdateOneAsync(p => p.Id == item.ProductId, stockUpdate);
        }

        var updatedOrder = await _orders.Find(o => o.Id == id).FirstOrDefaultAsync();
        return Ok(updatedOrder);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("stats")]
    public async Task<ActionResult<OrderStats>> GetOrderStats()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        var stats = new OrderStats
        {
            TotalOrders = (int)await _orders.CountDocumentsAsync(_ => true),
            PendingOrders = (int)await _orders.CountDocumentsAsync(o => o.Status == OrderStatus.Pending),
            ProcessingOrders = (int)await _orders.CountDocumentsAsync(o => o.Status == OrderStatus.Processing),
            ShippedOrders = (int)await _orders.CountDocumentsAsync(o => o.Status == OrderStatus.Shipped),
            DeliveredOrders = (int)await _orders.CountDocumentsAsync(o => o.Status == OrderStatus.Delivered),
            CancelledOrders = (int)await _orders.CountDocumentsAsync(o => o.Status == OrderStatus.Cancelled)
        };

        // Gelir istatistikleri
        var allOrders = await _orders.Find(o => o.PaymentStatus == PaymentStatus.Paid).ToListAsync();
        stats.TotalRevenue = allOrders.Sum(o => o.Total);

        var todayOrders = allOrders.Where(o => o.CreatedAt.Date == today);
        stats.TodayRevenue = todayOrders.Sum(o => o.Total);

        var monthOrders = allOrders.Where(o => o.CreatedAt >= monthStart);
        stats.MonthRevenue = monthOrders.Sum(o => o.Total);

        return Ok(stats);
    }

    private string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"ORD-{timestamp}-{random}";
    }
}