using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;
using System.Text.RegularExpressions;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    private readonly IMongoCollection<Brand> _brands;

    public BrandsController(IMongoDatabase database)
    {
        _brands = database.GetCollection<Brand>("Brands");
    }

    [HttpGet]
    public async Task<ActionResult<List<Brand>>> GetBrands()
    {
        var brands = await _brands
            .Find(b => b.IsActive)
            .SortBy(b => b.Name)
            .ToListAsync();
        
        return Ok(brands);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Brand>> GetBrand(string id)
    {
        var brand = await _brands.Find(b => b.Id == id).FirstOrDefaultAsync();
        
        if (brand == null)
            return NotFound(new { message = "Marka bulunamadı." });

        return Ok(brand);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Brand>> GetBrandBySlug(string slug)
    {
        var brand = await _brands.Find(b => b.Slug == slug).FirstOrDefaultAsync();
        
        if (brand == null)
            return NotFound(new { message = "Marka bulunamadı." });

        return Ok(brand);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Brand>> CreateBrand([FromBody] CreateBrandRequest request)
    {
        var brand = new Brand
        {
            Name = request.Name,
            Slug = GenerateSlug(request.Name),
            Description = request.Description,
            Logo = request.Logo,
            CreatedAt = DateTime.UtcNow
        };

        await _brands.InsertOneAsync(brand);
        return CreatedAtAction(nameof(GetBrand), new { id = brand.Id }, brand);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<Brand>> UpdateBrand(string id, [FromBody] CreateBrandRequest request)
    {
        var brand = await _brands.Find(b => b.Id == id).FirstOrDefaultAsync();
        if (brand == null)
            return NotFound(new { message = "Marka bulunamadı." });

        var update = Builders<Brand>.Update
            .Set(b => b.Name, request.Name)
            .Set(b => b.Slug, GenerateSlug(request.Name))
            .Set(b => b.Description, request.Description)
            .Set(b => b.Logo, request.Logo);

        await _brands.UpdateOneAsync(b => b.Id == id, update);
        
        var updatedBrand = await _brands.Find(b => b.Id == id).FirstOrDefaultAsync();
        return Ok(updatedBrand);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBrand(string id)
    {
        var result = await _brands.DeleteOneAsync(b => b.Id == id);
        
        if (result.DeletedCount == 0)
            return NotFound(new { message = "Marka bulunamadı." });

        return Ok(new { message = "Marka silindi." });
    }

    private string GenerateSlug(string text)
    {
        text = text.ToLowerInvariant();
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        text = text.Replace(" ", "-");
        text = Regex.Replace(text, @"-+", "-");
        text = text.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                   .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        return text;
    }
}