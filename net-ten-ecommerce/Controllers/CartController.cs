using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;
using System.Security.Claims;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly IMongoCollection<Cart> _carts;
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Coupon> _coupons;
    private const decimal TAX_RATE = 0.20m; // KDV %20
    private const decimal FREE_SHIPPING_THRESHOLD = 500m;
    private const decimal SHIPPING_COST = 29.99m;

    public CartController(IMongoDatabase database)
    {
        _carts = database.GetCollection<Cart>("Carts");
        _products = database.GetCollection<Product>("Products");
        _coupons = database.GetCollection<Coupon>("Coupons");
    }

    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCart()
    {
        var cart = await GetOrCreateCart();
        return Ok(MapToCartResponse(cart));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartResponse>> AddToCart([FromBody] AddToCartRequest request)
    {
        if (request.Quantity <= 0)
            return BadRequest(new { message = "Miktar 0'dan büyük olmalıdır." });

        var product = await _products.Find(p => p.Id == request.ProductId && p.IsActive).FirstOrDefaultAsync();
        if (product == null)
            return NotFound(new { message = "Ürün bulunamadı." });

        if (product.Stock < request.Quantity)
            return BadRequest(new { message = "Yetersiz stok." });

        var cart = await GetOrCreateCart();

        // Aynı ürün sepette var mı kontrol et
        var existingItem = cart.Items.FirstOrDefault(i => 
            i.ProductId == request.ProductId && i.Variant == request.Variant);

        if (existingItem != null)
        {
            // Mevcut miktara ekle
            var newQuantity = existingItem.Quantity + request.Quantity;
            if (product.Stock < newQuantity)
                return BadRequest(new { message = "Yetersiz stok." });

            existingItem.Quantity = newQuantity;
            existingItem.Subtotal = (existingItem.DiscountPrice ?? existingItem.Price) * newQuantity;
        }
        else
        {
            // Yeni ürün ekle
            var cartItem = new CartItem
            {
                ProductId = product.Id!,
                ProductName = product.Name,
                ProductImage = product.Images.FirstOrDefault() ?? string.Empty,
                SKU = product.SKU,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                Quantity = request.Quantity,
                Variant = request.Variant,
                Subtotal = (product.DiscountPrice ?? product.Price) * request.Quantity
            };
            cart.Items.Add(cartItem);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await RecalculateCart(cart);
        await SaveCart(cart);

        return Ok(MapToCartResponse(cart));
    }

    [HttpPut("items/{productId}")]
    public async Task<ActionResult<CartResponse>> UpdateCartItem(
        string productId, 
        [FromBody] UpdateCartItemRequest request)
    {
        if (request.Quantity < 0)
            return BadRequest(new { message = "Miktar 0'dan küçük olamaz." });

        var cart = await GetOrCreateCart();
        var item = cart.Items.FirstOrDefault(i => 
            i.ProductId == productId && i.Variant == request.Variant);

        if (item == null)
            return NotFound(new { message = "Ürün sepette bulunamadı." });

        if (request.Quantity == 0)
        {
            // Ürünü sepetten çıkar
            cart.Items.Remove(item);
        }
        else
        {
            // Stok kontrolü
            var product = await _products.Find(p => p.Id == productId).FirstOrDefaultAsync();
            if (product == null)
                return NotFound(new { message = "Ürün bulunamadı." });

            if (product.Stock < request.Quantity)
                return BadRequest(new { message = "Yetersiz stok." });

            item.Quantity = request.Quantity;
            item.Subtotal = (item.DiscountPrice ?? item.Price) * request.Quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await RecalculateCart(cart);
        await SaveCart(cart);

        return Ok(MapToCartResponse(cart));
    }

    [HttpDelete("items/{productId}")]
    public async Task<ActionResult<CartResponse>> RemoveFromCart(string productId, [FromQuery] string? variant = null)
    {
        var cart = await GetOrCreateCart();
        var item = cart.Items.FirstOrDefault(i => 
            i.ProductId == productId && i.Variant == variant);

        if (item == null)
            return NotFound(new { message = "Ürün sepette bulunamadı." });

        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await RecalculateCart(cart);
        await SaveCart(cart);

        return Ok(MapToCartResponse(cart));
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        var cart = await GetOrCreateCart();
        cart.Items.Clear();
        cart.CouponCode = null;
        cart.Discount = 0;
        cart.UpdatedAt = DateTime.UtcNow;
        await RecalculateCart(cart);
        await SaveCart(cart);

        return Ok(new { message = "Sepet temizlendi." });
    }

    [HttpPost("coupon")]
    public async Task<ActionResult<CartResponse>> ApplyCoupon([FromBody] ApplyCouponRequest request)
    {
        var cart = await GetOrCreateCart();

        var coupon = await _coupons.Find(c => 
            c.Code.ToLower() == request.CouponCode.ToLower() && 
            c.IsActive
        ).FirstOrDefaultAsync();

        if (coupon == null)
            return BadRequest(new { message = "Geçersiz kupon kodu." });

        // Kupon geçerlilik kontrolü
        var now = DateTime.UtcNow;
        if (now < coupon.ValidFrom || now > coupon.ValidUntil)
            return BadRequest(new { message = "Kupon süresi geçmiş veya henüz aktif değil." });

        // Kullanım limiti kontrolü
        if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
            return BadRequest(new { message = "Kupon kullanım limitine ulaşmış." });

        // Minimum alışveriş tutarı kontrolü
        if (cart.Subtotal < coupon.MinPurchaseAmount)
            return BadRequest(new { message = $"Bu kuponu kullanmak için minimum {coupon.MinPurchaseAmount:C2} alışveriş yapmalısınız." });

        cart.CouponCode = coupon.Code;
        cart.UpdatedAt = DateTime.UtcNow;
        await RecalculateCart(cart);
        await SaveCart(cart);

        return Ok(MapToCartResponse(cart));
    }

    [HttpDelete("coupon")]
    public async Task<ActionResult<CartResponse>> RemoveCoupon()
    {
        var cart = await GetOrCreateCart();
        cart.CouponCode = null;
        cart.Discount = 0;
        cart.UpdatedAt = DateTime.UtcNow;
        await RecalculateCart(cart);
        await SaveCart(cart);

        return Ok(MapToCartResponse(cart));
    }

    private async Task<Cart> GetOrCreateCart()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var sessionId = HttpContext.Session.Id;

        Cart? cart = null;

        if (!string.IsNullOrEmpty(userId))
        {
            // Kayıtlı kullanıcı için sepet bul
            cart = await _carts.Find(c => c.UserId == userId).FirstOrDefaultAsync();
            
            // Session sepeti varsa birleştir
            if (cart == null)
            {
                var sessionCart = await _carts.Find(c => c.SessionId == sessionId).FirstOrDefaultAsync();
                if (sessionCart != null)
                {
                    sessionCart.UserId = userId;
                    sessionCart.SessionId = null;
                    cart = sessionCart;
                }
            }
        }
        else
        {
            // Misafir kullanıcı için session sepeti bul
            cart = await _carts.Find(c => c.SessionId == sessionId).FirstOrDefaultAsync();
        }

        if (cart == null)
        {
            cart = new Cart
            {
                UserId = userId,
                SessionId = string.IsNullOrEmpty(userId) ? sessionId : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _carts.InsertOneAsync(cart);
        }

        return cart;
    }

    private async Task RecalculateCart(Cart cart)
    {
        // Subtotal hesapla
        cart.Subtotal = cart.Items.Sum(i => i.Subtotal);

        // Kupon indirimi hesapla
        cart.Discount = 0;
        if (!string.IsNullOrEmpty(cart.CouponCode))
        {
            var coupon = await _coupons.Find(c => c.Code == cart.CouponCode && c.IsActive)
                .FirstOrDefaultAsync();

            if (coupon != null)
            {
                cart.Discount = coupon.DiscountType == DiscountType.Percentage
                    ? cart.Subtotal * (coupon.DiscountValue / 100)
                    : coupon.DiscountValue;

                if (coupon.MaxDiscountAmount.HasValue && cart.Discount > coupon.MaxDiscountAmount.Value)
                    cart.Discount = coupon.MaxDiscountAmount.Value;
            }
        }

        // Kargo ücreti hesapla
        var subtotalAfterDiscount = cart.Subtotal - cart.Discount;
        cart.ShippingCost = subtotalAfterDiscount >= FREE_SHIPPING_THRESHOLD ? 0 : SHIPPING_COST;

        // Vergi hesapla (KDV indirim sonrası tutardan)
        cart.Tax = subtotalAfterDiscount * TAX_RATE;

        // Toplam hesapla
        cart.Total = subtotalAfterDiscount + cart.Tax + cart.ShippingCost;
    }

    private async Task SaveCart(Cart cart)
    {
        await _carts.ReplaceOneAsync(c => c.Id == cart.Id, cart);
    }

    private CartResponse MapToCartResponse(Cart cart)
    {
        return new CartResponse
        {
            Id = cart.Id,
            Items = cart.Items.Select(i => new CartItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductImage = i.ProductImage,
                SKU = i.SKU,
                Price = i.Price,
                DiscountPrice = i.DiscountPrice,
                FinalPrice = i.DiscountPrice ?? i.Price,
                Quantity = i.Quantity,
                Variant = i.Variant,
                Subtotal = i.Subtotal,
                InStock = true, // Bu değer gerçek stok kontrolü ile güncellenebilir
                AvailableStock = 100 // Bu değer gerçek stok bilgisi ile güncellenebilir
            }).ToList(),
            CouponCode = cart.CouponCode,
            Discount = cart.Discount,
            Subtotal = cart.Subtotal,
            Tax = cart.Tax,
            ShippingCost = cart.ShippingCost,
            Total = cart.Total,
            ItemCount = cart.Items.Sum(i => i.Quantity),
            UpdatedAt = cart.UpdatedAt
        };
    }
}