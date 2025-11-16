using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;
using System.Security.Claims;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Role> _roles;

    public UsersController(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("Users");
        _roles = database.GetCollection<Role>("Roles");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers([FromQuery] string? search = null)
    {
        var filter = Builders<User>.Filter.Empty;

        if (!string.IsNullOrEmpty(search))
        {
            filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.FullName, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(search, "i"))
            );
        }

        var users = await _users.Find(filter)
            .SortByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id!,
            Email = u.Email,
            FullName = u.FullName,
            Phone = u.Phone,
            Roles = u.Roles.Select(r => r.RoleName).ToList(),
            IsActive = u.IsActive,
            IsEmailVerified = u.IsEmailVerified,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt
        }).ToList();

        return Ok(userDtos);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        return Ok(new UserDto
        {
            Id = user.Id!,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Roles = user.Roles.Select(r => r.RoleName).ToList(),
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(string id)
    {
        var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        return Ok(new UserDto
        {
            Id = user.Id!,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Roles = user.Roles.Select(r => r.RoleName).ToList(),
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{userId}/roles")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
    {
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        var role = await _roles.Find(r => r.Name == request.RoleName).FirstOrDefaultAsync();
        if (role == null)
            return NotFound(new { message = "Rol bulunamadı." });

        // Kullanıcının zaten bu rolü var mı kontrol et
        if (user.Roles.Any(r => r.RoleName == request.RoleName))
            return BadRequest(new { message = "Kullanıcı zaten bu role sahip." });

        var userRole = new UserRole
        {
            RoleId = role.Id!,
            RoleName = role.Name,
            AssignedAt = DateTime.UtcNow
        };

        var update = Builders<User>.Update
            .Push(u => u.Roles, userRole)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await _users.UpdateOneAsync(u => u.Id == userId, update);

        return Ok(new { message = $"{role.DisplayName} rolü başarıyla atandı." });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{userId}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(string userId, string roleName)
    {
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        var userRole = user.Roles.FirstOrDefault(r => r.RoleName == roleName);
        if (userRole == null)
            return NotFound(new { message = "Kullanıcının bu rolü yok." });

        // En az bir rol kalmalı (Customer)
        if (user.Roles.Count == 1)
            return BadRequest(new { message = "Kullanıcının en az bir rolü olmalıdır." });

        var update = Builders<User>.Update
            .Pull(u => u.Roles, userRole)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await _users.UpdateOneAsync(u => u.Id == userId, update);

        return Ok(new { message = "Rol başarıyla kaldırıldı." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{userId}/activate")]
    public async Task<IActionResult> ToggleUserStatus(string userId)
    {
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        var update = Builders<User>.Update
            .Set(u => u.IsActive, !user.IsActive)
            .Set(u => u.UpdatedAt, DateTime.UtcNow);

        await _users.UpdateOneAsync(u => u.Id == userId, update);

        return Ok(new { message = "Kullanıcı durumu güncellendi.", isActive = !user.IsActive });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        var updateBuilder = Builders<User>.Update;
        var updates = new List<UpdateDefinition<User>>();

        if (!string.IsNullOrEmpty(request.FullName))
            updates.Add(updateBuilder.Set(u => u.FullName, request.FullName));

        if (!string.IsNullOrEmpty(request.Phone))
            updates.Add(updateBuilder.Set(u => u.Phone, request.Phone));

        updates.Add(updateBuilder.Set(u => u.UpdatedAt, DateTime.UtcNow));

        var combinedUpdate = updateBuilder.Combine(updates);
        await _users.UpdateOneAsync(u => u.Id == userId, combinedUpdate);

        return Ok(new { message = "Profil güncellendi." });
    }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssignRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
}