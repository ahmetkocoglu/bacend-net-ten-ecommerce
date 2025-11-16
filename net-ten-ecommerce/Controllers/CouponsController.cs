using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CouponsController : ControllerBase
{
    private readonly IMongoCollection<Coupon> _coupons;

    public CouponsController(IMongoDatabase database)
    {
        _coupons = database.GetCollection<Coupon>("Coupons");
    }

    [HttpGet]
    public async Task<ActionResult<List<Coupon>>> GetCoupons()
    {
        var coupons = await _coupons
            .Find(_ => true)
            .SortByDescending(c => c.CreatedAt)
            .ToListAsync();
        
        return Ok(coupons);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Coupon>> GetCoupon(string id)
    {
        var coupon = await _coupons.Find(c => c.Id == id).FirstOrDefaultAsync();
        
        if (coupon == null)
            return NotFound(new { message = "Kupon bulunamadı." });

        return Ok(coupon);
    }

    [HttpGet("validate/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult> ValidateCoupon(string code)
    {
        var coupon = await _coupons.Find(c => 
            c.Code.ToLower() == code.ToLower() && 
            c.IsActive
        ).FirstOrDefaultAsync();

        if (coupon == null)
            return BadRequest(new { valid = false, message = "Geçersiz kupon kodu." });

        var now = DateTime.UtcNow;
        if (now < coupon.ValidFrom || now > coupon.ValidUntil)
            return BadRequest(new { valid = false, message = "Kupon süresi geçmiş veya henüz aktif değil." });

        if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
            return BadRequest(new { valid = false, message = "Kupon kullanım limitine ulaşmış." });

        return Ok(new 
        { 
            valid = true, 
            coupon = new
            {
                code = coupon.Code,
                description = coupon.Description,
                discountType = coupon.DiscountType.ToString(),
                discountValue = coupon.DiscountValue,
                minPurchaseAmount = coupon.MinPurchaseAmount,
                maxDiscountAmount = coupon.MaxDiscountAmount
            }
        });
    }

    [HttpPost]
    public async Task<ActionResult<Coupon>> CreateCoupon([FromBody] CreateCouponRequest request)
    {
        // Kupon kodu benzersizliği kontrolü
        var existing = await _coupons.Find(c => c.Code.ToLower() == request.Code.ToLower())
            .FirstOrDefaultAsync();

        if (existing != null)
            return BadRequest(new { message = "Bu kupon kodu zaten kullanılıyor." });

        var coupon = new Coupon
        {
            Code = request.Code.ToUpper(),
            Description = request.Description,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinPurchaseAmount = request.MinPurchaseAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            UsageLimit = request.UsageLimit,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            CreatedAt = DateTime.UtcNow
        };

        await _coupons.InsertOneAsync(coupon);
        return CreatedAtAction(nameof(GetCoupon), new { id = coupon.Id }, coupon);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Coupon>> UpdateCoupon(string id, [FromBody] CreateCouponRequest request)
    {
        var coupon = await _coupons.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (coupon == null)
            return NotFound(new { message = "Kupon bulunamadı." });

        var update = Builders<Coupon>.Update
            .Set(c => c.Code, request.Code.ToUpper())
            .Set(c => c.Description, request.Description)
            .Set(c => c.DiscountType, request.DiscountType)
            .Set(c => c.DiscountValue, request.DiscountValue)
            .Set(c => c.MinPurchaseAmount, request.MinPurchaseAmount)
            .Set(c => c.MaxDiscountAmount, request.MaxDiscountAmount)
            .Set(c => c.UsageLimit, request.UsageLimit)
            .Set(c => c.ValidFrom, request.ValidFrom)
            .Set(c => c.ValidUntil, request.ValidUntil);

        await _coupons.UpdateOneAsync(c => c.Id == id, update);
        
        var updatedCoupon = await _coupons.Find(c => c.Id == id).FirstOrDefaultAsync();
        return Ok(updatedCoupon);
    }

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> ToggleCouponStatus(string id)
    {
        var coupon = await _coupons.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (coupon == null)
            return NotFound(new { message = "Kupon bulunamadı." });

        var update = Builders<Coupon>.Update.Set(c => c.IsActive, !coupon.IsActive);
        await _coupons.UpdateOneAsync(c => c.Id == id, update);

        return Ok(new { message = "Kupon durumu güncellendi.", isActive = !coupon.IsActive });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCoupon(string id)
    {
        var result = await _coupons.DeleteOneAsync(c => c.Id == id);
        
        if (result.DeletedCount == 0)
            return NotFound(new { message = "Kupon bulunamadı." });

        return Ok(new { message = "Kupon silindi." });
    }
}