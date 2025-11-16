using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;
using System.Security.Claims;

namespace net_ten_ecommerce.Controllers;

public class UserAddressesController : ControllerBase
{
    private readonly IMongoCollection<UserAddress> _userAddresses;

    public UserAddressesController(IMongoDatabase database)
    {
        _userAddresses = database.GetCollection<UserAddress>("UserAddresses");
    }

    [HttpGet]
    public async Task<ActionResult<List<UserAddress>>> GetMyAddresses()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var addresses = await _userAddresses
            .Find(a => a.UserId == userId)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(addresses);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        // Varsayılan adres ayarlanacaksa diğerlerini sıfırla
        var updates = new List<UpdateDefinition<UserAddress>>();
        var updateBuilder = Builders<UserAddress>.Update;

        if (request.IsDefaultShipping)
        {
            await _userAddresses.UpdateManyAsync(
                a => a.UserId == userId,
                updateBuilder.Set(a => a.IsDefaultShipping, false)
            );
        }

        if (request.IsDefaultBilling)
        {
            await _userAddresses.UpdateManyAsync(
                a => a.UserId == userId,
                updateBuilder.Set(a => a.IsDefaultBilling, false)
            );
        }

        var entity = new UserAddress
        {
            UserId = userId,
            Address = request.Address,
            Label = request.Label,
            AddressType = request.AddressType,
            IsDefaultShipping = request.IsDefaultShipping,
            IsDefaultBilling = request.IsDefaultBilling
        };

        await _userAddresses.InsertOneAsync(entity);

        return CreatedAtAction(nameof(GetAddressById), new { id = entity.Id }, entity);
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<UserAddress>> GetAddressById(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var address = await _userAddresses
            .Find(a => a.Id == id && a.UserId == userId)
            .FirstOrDefaultAsync();

        if (address == null)
            return NotFound();

        return Ok(address);
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAddress(string id, [FromBody] UpdateAddressRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var address = await _userAddresses
            .Find(a => a.Id == id && a.UserId == userId)
            .FirstOrDefaultAsync();

        if (address == null)
            return NotFound();

        var updateBuilder = Builders<UserAddress>.Update;
        var updates = new List<UpdateDefinition<UserAddress>>();

        if (request.Address is not null)
            updates.Add(updateBuilder.Set(a => a.Address, request.Address));

        if (!string.IsNullOrEmpty(request.Label))
            updates.Add(updateBuilder.Set(a => a.Label, request.Label));

        if (request.AddressType.HasValue)
            updates.Add(updateBuilder.Set(a => a.AddressType, request.AddressType.Value));

        if (request.IsDefaultShipping.HasValue && request.IsDefaultShipping.Value)
        {
            await _userAddresses.UpdateManyAsync(
                a => a.UserId == userId,
                updateBuilder.Set(a => a.IsDefaultShipping, false)
            );
            updates.Add(updateBuilder.Set(a => a.IsDefaultShipping, true));
        }

        if (request.IsDefaultBilling.HasValue && request.IsDefaultBilling.Value)
        {
            await _userAddresses.UpdateManyAsync(
                a => a.UserId == userId,
                updateBuilder.Set(a => a.IsDefaultBilling, false)
            );
            updates.Add(updateBuilder.Set(a => a.IsDefaultBilling, true));
        }

        updates.Add(updateBuilder.Set(a => a.UpdatedAt, DateTime.UtcNow));

        if (!updates.Any())
            return BadRequest(new { message = "Güncellenecek alan yok." });

        var combined = updateBuilder.Combine(updates);
        await _userAddresses.UpdateOneAsync(a => a.Id == id && a.UserId == userId, combined);

        return Ok(new { message = "Adres güncellendi." });
    }
    
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAddress(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return Unauthorized();

        var result = await _userAddresses.DeleteOneAsync(
            a => a.Id == id && a.UserId == userId
        );

        if (result.DeletedCount == 0)
            return NotFound();

        return Ok(new { message = "Adres silindi." });
    }
}