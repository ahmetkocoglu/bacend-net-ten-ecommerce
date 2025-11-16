namespace net_ten_ecommerce.Models;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string CategoryId { get; set; } = string.Empty;
    public string? BrandId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int Stock { get; set; }
    public List<string> Images { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, string> Specifications { get; set; } = new();
    public List<ProductVariant> Variants { get; set; } = new();
    public bool IsFeatured { get; set; } = false;
}

public class UpdateProductRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal? Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string? CategoryId { get; set; }
    public string? BrandId { get; set; }
    public int? Stock { get; set; }
    public List<string>? Images { get; set; }
    public List<string>? Tags { get; set; }
    public Dictionary<string, string>? Specifications { get; set; }
    public List<ProductVariant>? Variants { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsFeatured { get; set; }
}

public class ProductFilterRequest
{
    public string? Search { get; set; }
    public string? CategoryId { get; set; }
    public string? BrandId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsFeatured { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortOrder { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ProductListResponse
{
    public List<ProductSummary> Products { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class ProductSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public string MainImage { get; set; } = string.Empty;
    public int Stock { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string? Image { get; set; }
    public int Order { get; set; } = 0;
}

public class CreateBrandRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Logo { get; set; }
}