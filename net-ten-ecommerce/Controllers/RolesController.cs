using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly IMongoCollection<Role> _roles;

    public RolesController(IMongoDatabase database)
    {
        _roles = database.GetCollection<Role>("Roles");
    }

    [HttpGet]
    public async Task<ActionResult<List<Role>>> GetRoles()
    {
        var roles = await _roles
            .Find(r => r.IsActive)
            .SortBy(r => r.Name)
            .ToListAsync();

        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Role>> GetRole(string id)
    {
        var role = await _roles.Find(r => r.Id == id).FirstOrDefaultAsync();

        if (role == null)
            return NotFound(new { message = "Rol bulunamadı." });

        return Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult<Role>> CreateRole([FromBody] CreateRoleRequest request)
    {
        // Rol adı benzersizliği kontrolü
        var existing = await _roles.Find(r => r.Name == request.Name).FirstOrDefaultAsync();
        if (existing != null)
            return BadRequest(new { message = "Bu rol adı zaten kullanılıyor." });

        var role = new Role
        {
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            Permissions = request.Permissions,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _roles.InsertOneAsync(role);
        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, role);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Role>> UpdateRole(string id, [FromBody] CreateRoleRequest request)
    {
        var role = await _roles.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (role == null)
            return NotFound(new { message = "Rol bulunamadı." });

        var update = Builders<Role>.Update
            .Set(r => r.DisplayName, request.DisplayName)
            .Set(r => r.Description, request.Description)
            .Set(r => r.Permissions, request.Permissions);

        await _roles.UpdateOneAsync(r => r.Id == id, update);

        var updatedRole = await _roles.Find(r => r.Id == id).FirstOrDefaultAsync();
        return Ok(updatedRole);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roles.Find(r => r.Id == id).FirstOrDefaultAsync();
        if (role == null)
            return NotFound(new { message = "Rol bulunamadı." });

        // Sistem rolleri silinemez
        if (role.Name == Roles.Admin || role.Name == Roles.Customer)
            return BadRequest(new { message = "Sistem rolleri silinemez." });

        var result = await _roles.DeleteOneAsync(r => r.Id == id);

        if (result.DeletedCount == 0)
            return NotFound(new { message = "Rol bulunamadı." });

        return Ok(new { message = "Rol silindi." });
    }

    [HttpGet("permissions")]
    public ActionResult<object> GetAvailablePermissions()
    {
        var permissions = new
        {
            product = new[] { 
                Permissions.ProductCreate, 
                Permissions.ProductEdit, 
                Permissions.ProductDelete, 
                Permissions.ProductView 
            },
            order = new[] { 
                Permissions.OrderView, 
                Permissions.OrderEdit, 
                Permissions.OrderCancel, 
                Permissions.OrderRefund 
            },
            user = new[] { 
                Permissions.UserCreate, 
                Permissions.UserEdit, 
                Permissions.UserDelete, 
                Permissions.UserView 
            },
            cargo = new[] { 
                Permissions.CargoCreate, 
                Permissions.CargoCancel, 
                Permissions.CargoView 
            },
            coupon = new[] { 
                Permissions.CouponCreate, 
                Permissions.CouponEdit, 
                Permissions.CouponDelete 
            },
            reports = new[] { 
                Permissions.ReportsView, 
                Permissions.ReportsExport 
            }
        };

        return Ok(permissions);
    }
}

public class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}