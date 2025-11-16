
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;
using System.Text.RegularExpressions;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMongoCollection<Category> _categories;

    public CategoriesController(IMongoDatabase database)
    {
        _categories = database.GetCollection<Category>("Categories");
    }

    [HttpGet]
    public async Task<ActionResult<List<Category>>> GetCategories()
    {
        var categories = await _categories
            .Find(c => c.IsActive)
            .SortBy(c => c.Order)
            .ToListAsync();
        
        return Ok(categories);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetCategory(string id)
    {
        var category = await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
        
        if (category == null)
            return NotFound(new { message = "Kategori bulunamadı." });

        return Ok(category);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Category>> GetCategoryBySlug(string slug)
    {
        var category = await _categories.Find(c => c.Slug == slug).FirstOrDefaultAsync();
        
        if (category == null)
            return NotFound(new { message = "Kategori bulunamadı." });

        return Ok(category);
    }

    [HttpGet("{id}/subcategories")]
    public async Task<ActionResult<List<Category>>> GetSubcategories(string id)
    {
        var subcategories = await _categories
            .Find(c => c.ParentId == id && c.IsActive)
            .SortBy(c => c.Order)
            .ToListAsync();
        
        return Ok(subcategories);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Category>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        // Parent kategori kontrolü
        if (!string.IsNullOrEmpty(request.ParentId))
        {
            var parent = await _categories.Find(c => c.Id == request.ParentId).FirstOrDefaultAsync();
            if (parent == null)
                return BadRequest(new { message = "Geçersiz üst kategori." });
        }

        var category = new Category
        {
            Name = request.Name,
            Slug = GenerateSlug(request.Name),
            Description = request.Description,
            ParentId = request.ParentId,
            Image = request.Image,
            Order = request.Order,
            CreatedAt = DateTime.UtcNow
        };

        await _categories.InsertOneAsync(category);
        return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<Category>> UpdateCategory(string id, [FromBody] CreateCategoryRequest request)
    {
        var category = await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
        if (category == null)
            return NotFound(new { message = "Kategori bulunamadı." });

        var update = Builders<Category>.Update
            .Set(c => c.Name, request.Name)
            .Set(c => c.Slug, GenerateSlug(request.Name))
            .Set(c => c.Description, request.Description)
            .Set(c => c.ParentId, request.ParentId)
            .Set(c => c.Image, request.Image)
            .Set(c => c.Order, request.Order);

        await _categories.UpdateOneAsync(c => c.Id == id, update);
        
        var updatedCategory = await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
        return Ok(updatedCategory);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        // Alt kategorileri kontrol et
        var hasSubcategories = await _categories
            .Find(c => c.ParentId == id)
            .AnyAsync();

        if (hasSubcategories)
            return BadRequest(new { message = "Bu kategorinin alt kategorileri var. Önce onları silin." });

        var result = await _categories.DeleteOneAsync(c => c.Id == id);
        
        if (result.DeletedCount == 0)
            return NotFound(new { message = "Kategori bulunamadı." });

        return Ok(new { message = "Kategori silindi." });
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