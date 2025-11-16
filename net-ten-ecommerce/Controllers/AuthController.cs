using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using net_ten_ecommerce.Models;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMongoDatabase _database;
    private readonly IConfiguration _configuration;
    private readonly IMongoCollection<User> _usersCollection;
    private readonly IMongoCollection<Role> _rolesCollection;

    public AuthController(IMongoDatabase database, IConfiguration configuration)
    {
        _database = database;
        _configuration = configuration;
        _usersCollection = _database.GetCollection<User>("Users");
        _rolesCollection = _database.GetCollection<Role>("Roles");
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Email kontrolü
        var existingUser = await _usersCollection
            .Find(u => u.Email == request.Email)
            .FirstOrDefaultAsync();

        if (existingUser != null)
        {
            return BadRequest(new { message = "Bu email adresi zaten kullanılıyor." });
        }

        // Şifreyi hash'le
        var hashedPassword = HashPassword(request.Password);

        // Varsayılan "Customer" rolünü al
        var customerRole = await _rolesCollection
            .Find(r => r.Name == Roles.Customer)
            .FirstOrDefaultAsync();

        if (customerRole == null)
        {
            // Eğer roller henüz oluşturulmamışsa, oluştur
            await InitializeRoles();
            customerRole = await _rolesCollection
                .Find(r => r.Name == Roles.Customer)
                .FirstOrDefaultAsync();
        }

        var user = new User
        {
            Email = request.Email,
            Password = hashedPassword,
            FullName = request.FullName,
            Phone = request.Phone,
            Roles = new List<UserRole>
            {
                new UserRole
                {
                    RoleId = customerRole!.Id!,
                    RoleName = customerRole.Name,
                    AssignedAt = DateTime.UtcNow
                }
            },
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _usersCollection.InsertOneAsync(user);

        return Ok(new { message = "Kayıt başarılı!", userId = user.Id });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _usersCollection
            .Find(u => u.Email == request.Email)
            .FirstOrDefaultAsync();

        if (user == null || !VerifyPassword(request.Password, user.Password))
        {
            return Unauthorized(new { message = "Email veya şifre hatalı." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Hesabınız aktif değil." });
        }

        // Son giriş zamanını güncelle
        var update = Builders<User>.Update.Set(u => u.LastLoginAt, DateTime.UtcNow);
        await _usersCollection.UpdateOneAsync(u => u.Id == user.Id, update);

        var token = GenerateJwtToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            Email = user.Email,
            FullName = user.FullName,
            Roles = user.Roles.Select(r => r.RoleName).ToList()
        });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var user = await _usersCollection
            .Find(u => u.Id == userId)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            fullName = user.FullName,
            createdAt = user.CreatedAt
        });
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName)
        };

        // Rolleri claim olarak ekle
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
        }

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hashedPassword)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hashedPassword;
    }

    private async Task InitializeRoles()
    {
        var existingRoles = await _rolesCollection.Find(_ => true).ToListAsync();
        if (existingRoles.Any()) return;

        var roles = new List<Role>
        {
            new Role
            {
                Name = Roles.Admin,
                DisplayName = "Yönetici",
                Description = "Tüm sistem yetkilerine sahip",
                Permissions = new List<string>
                {
                    Permissions.ProductCreate, Permissions.ProductEdit, Permissions.ProductDelete, Permissions.ProductView,
                    Permissions.OrderView, Permissions.OrderEdit, Permissions.OrderCancel, Permissions.OrderRefund,
                    Permissions.UserCreate, Permissions.UserEdit, Permissions.UserDelete, Permissions.UserView,
                    Permissions.CargoCreate, Permissions.CargoCancel, Permissions.CargoView,
                    Permissions.CouponCreate, Permissions.CouponEdit, Permissions.CouponDelete,
                    Permissions.ReportsView, Permissions.ReportsExport
                }
            },
            new Role
            {
                Name = Roles.Customer,
                DisplayName = "Müşteri",
                Description = "Normal müşteri kullanıcısı",
                Permissions = new List<string>
                {
                    Permissions.ProductView,
                    Permissions.OrderView
                }
            },
            new Role
            {
                Name = Roles.Vendor,
                DisplayName = "Satıcı",
                Description = "Ürün satıcısı",
                Permissions = new List<string>
                {
                    Permissions.ProductCreate, Permissions.ProductEdit, Permissions.ProductView,
                    Permissions.OrderView,
                    Permissions.CargoView
                }
            },
            new Role
            {
                Name = Roles.Support,
                DisplayName = "Destek",
                Description = "Müşteri destek ekibi",
                Permissions = new List<string>
                {
                    Permissions.ProductView,
                    Permissions.OrderView, Permissions.OrderEdit,
                    Permissions.UserView,
                    Permissions.CargoView
                }
            }
        };

        await _rolesCollection.InsertManyAsync(roles);
    }
}