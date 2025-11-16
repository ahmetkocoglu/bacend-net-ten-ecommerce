using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using net_ten_ecommerce.Models;
using System.Text.RegularExpressions;

namespace net_ten_ecommerce.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMongoCollection<Product> _products;
    private readonly IMongoCollection<Category> _categories;
    private readonly IMongoCollection<Brand> _brands;

    public ProductsController(IMongoDatabase database)
    {
        _products = database.GetCollection<Product>("Products");
        _categories = database.GetCollection<Category>("Categories");
        _brands = database.GetCollection<Brand>("Brands");
    }

    [HttpGet]
    public async Task<ActionResult<ProductListResponse>> GetProducts([FromQuery] ProductFilterRequest filter)
    {
        var filterBuilder = Builders<Product>.Filter;
        var filters = new List<FilterDefinition<Product>>();

        // Arama
        if (!string.IsNullOrEmpty(filter.Search))
        {
            var searchFilter = filterBuilder.Or(
                filterBuilder.Regex(p => p.Name, new MongoDB.Bson.BsonRegularExpression(filter.Search, "i")),
                filterBuilder.Regex(p => p.Description, new MongoDB.Bson.BsonRegularExpression(filter.Search, "i"))
            );
            filters.Add(searchFilter);
        }

        // Kategori filtresi
        if (!string.IsNullOrEmpty(filter.CategoryId))
            filters.Add(filterBuilder.Eq(p => p.CategoryId, filter.CategoryId));

        // Marka filtresi
        if (!string.IsNullOrEmpty(filter.BrandId))
            filters.Add(filterBuilder.Eq(p => p.BrandId, filter.BrandId));

        // Fiyat aralığı
        if (filter.MinPrice.HasValue)
            filters.Add(filterBuilder.Gte(p => p.Price, filter.MinPrice.Value));
        
        if (filter.MaxPrice.HasValue)
            filters.Add(filterBuilder.Lte(p => p.Price, filter.MaxPrice.Value));

        // Tag filtresi
        if (filter.Tags != null && filter.Tags.Any())
            filters.Add(filterBuilder.AnyIn(p => p.Tags, filter.Tags));

        // Aktiflik durumu
        if (filter.IsActive.HasValue)
            filters.Add(filterBuilder.Eq(p => p.IsActive, filter.IsActive.Value));

        // Öne çıkan ürünler
        if (filter.IsFeatured.HasValue)
            filters.Add(filterBuilder.Eq(p => p.IsFeatured, filter.IsFeatured.Value));

        var finalFilter = filters.Any() 
            ? filterBuilder.And(filters) 
            : filterBuilder.Empty;

        // Toplam sayı
        var totalCount = await _products.CountDocumentsAsync(finalFilter);

        // Sıralama
        var sortDefinition = filter.SortOrder.ToLower() == "asc"
            ? Builders<Product>.Sort.Ascending(filter.SortBy)
            : Builders<Product>.Sort.Descending(filter.SortBy);

        // Sayfalama
        var products = await _products.Find(finalFilter)
            .Sort(sortDefinition)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Limit(filter.PageSize)
            .ToListAsync();

        var productSummaries = products.Select(p => new ProductSummary
        {
            Id = p.Id!,
            Name = p.Name,
            Slug = p.Slug,
            ShortDescription = p.ShortDescription,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            MainImage = p.Images.FirstOrDefault() ?? string.Empty,
            Stock = p.Stock,
            Rating = p.Rating,
            ReviewCount = p.ReviewCount,
            IsActive = p.IsActive,
            IsFeatured = p.IsFeatured
        }).ToList();

        return Ok(new ProductListResponse
        {
            Products = productSummaries,
            TotalCount = (int)totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(string id)
    {
        var product = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        
        if (product == null)
            return NotFound(new { message = "Ürün bulunamadı." });

        return Ok(product);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Product>> GetProductBySlug(string slug)
    {
        var product = await _products.Find(p => p.Slug == slug).FirstOrDefaultAsync();
        
        if (product == null)
            return NotFound(new { message = "Ürün bulunamadı." });

        return Ok(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct([FromBody] CreateProductRequest request)
    {
        // Kategori kontrolü
        var category = await _categories.Find(c => c.Id == request.CategoryId).FirstOrDefaultAsync();
        if (category == null)
            return BadRequest(new { message = "Geçersiz kategori." });

        // Marka kontrolü
        if (!string.IsNullOrEmpty(request.BrandId))
        {
            var brand = await _brands.Find(b => b.Id == request.BrandId).FirstOrDefaultAsync();
            if (brand == null)
                return BadRequest(new { message = "Geçersiz marka." });
        }

        var product = new Product
        {
            Name = request.Name,
            Slug = GenerateSlug(request.Name),
            Description = request.Description,
            ShortDescription = request.ShortDescription,
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            SKU = request.SKU,
            Stock = request.Stock,
            Images = request.Images,
            Tags = request.Tags,
            Specifications = request.Specifications,
            Variants = request.Variants,
            IsFeatured = request.IsFeatured,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _products.InsertOneAsync(product);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> UpdateProduct(string id, [FromBody] UpdateProductRequest request)
    {
        var product = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        if (product == null)
            return NotFound(new { message = "Ürün bulunamadı." });

        var updateBuilder = Builders<Product>.Update;
        var updates = new List<UpdateDefinition<Product>>();

        if (request.Name != null)
        {
            updates.Add(updateBuilder.Set(p => p.Name, request.Name));
            updates.Add(updateBuilder.Set(p => p.Slug, GenerateSlug(request.Name)));
        }
        if (request.Description != null) updates.Add(updateBuilder.Set(p => p.Description, request.Description));
        if (request.ShortDescription != null) updates.Add(updateBuilder.Set(p => p.ShortDescription, request.ShortDescription));
        if (request.Price.HasValue) updates.Add(updateBuilder.Set(p => p.Price, request.Price.Value));
        if (request.DiscountPrice.HasValue) updates.Add(updateBuilder.Set(p => p.DiscountPrice, request.DiscountPrice));
        if (request.CategoryId != null) updates.Add(updateBuilder.Set(p => p.CategoryId, request.CategoryId));
        if (request.BrandId != null) updates.Add(updateBuilder.Set(p => p.BrandId, request.BrandId));
        if (request.Stock.HasValue) updates.Add(updateBuilder.Set(p => p.Stock, request.Stock.Value));
        if (request.Images != null) updates.Add(updateBuilder.Set(p => p.Images, request.Images));
        if (request.Tags != null) updates.Add(updateBuilder.Set(p => p.Tags, request.Tags));
        if (request.Specifications != null) updates.Add(updateBuilder.Set(p => p.Specifications, request.Specifications));
        if (request.Variants != null) updates.Add(updateBuilder.Set(p => p.Variants, request.Variants));
        if (request.IsActive.HasValue) updates.Add(updateBuilder.Set(p => p.IsActive, request.IsActive.Value));
        if (request.IsFeatured.HasValue) updates.Add(updateBuilder.Set(p => p.IsFeatured, request.IsFeatured.Value));

        updates.Add(updateBuilder.Set(p => p.UpdatedAt, DateTime.UtcNow));

        if (updates.Any())
        {
            var combinedUpdate = updateBuilder.Combine(updates);
            await _products.UpdateOneAsync(p => p.Id == id, combinedUpdate);
        }

        var updatedProduct = await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        return Ok(updatedProduct);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        var result = await _products.DeleteOneAsync(p => p.Id == id);
        
        if (result.DeletedCount == 0)
            return NotFound(new { message = "Ürün bulunamadı." });

        return Ok(new { message = "Ürün silindi." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> UpdateStock(string id, [FromBody] int stock)
    {
        var update = Builders<Product>.Update
            .Set(p => p.Stock, stock)
            .Set(p => p.UpdatedAt, DateTime.UtcNow);

        var result = await _products.UpdateOneAsync(p => p.Id == id, update);
        
        if (result.MatchedCount == 0)
            return NotFound(new { message = "Ürün bulunamadı." });

        return Ok(new { message = "Stok güncellendi.", stock });
    }

    private string GenerateSlug(string text)
    {
        text = text.ToLowerInvariant();
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        text = text.Replace(" ", "-");
        text = Regex.Replace(text, @"-+", "-");
        
        // Türkçe karakter dönüşümü
        text = text.Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                   .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c");
        
        return text;
    }
}