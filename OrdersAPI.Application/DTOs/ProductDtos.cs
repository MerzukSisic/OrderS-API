namespace OrdersAPI.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public string PreparationLocation { get; set; } = string.Empty;
    public int PreparationTimeMinutes { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ProductIngredientDto> Ingredients { get; set; } = new();
    public List<AccompanimentGroupDto> AccompanimentGroups { get; set; } = new();
}

public class ProductSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public string PreparationLocation { get; set; } = string.Empty;
    public int PreparationTimeMinutes { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public string PreparationLocation { get; set; } = "Kitchen";
    public int PreparationTimeMinutes { get; set; } = 15;
    public int Stock { get; set; } = 0;
    public List<CreateProductIngredientDto> Ingredients { get; set; } = new();
}

public class UpdateProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public Guid? CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public bool? IsAvailable { get; set; }
    public string? PreparationLocation { get; set; }
    public int? PreparationTimeMinutes { get; set; }
    public int? Stock { get; set; }
    
    // ✅ ADD THESE TWO LINES:
    public List<CreateProductIngredientDto>? Ingredients { get; set; }
    public List<CreateAccompanimentGroupDto>? AccompanimentGroups { get; set; }
}

public class ProductIngredientDto
{
    public Guid Id { get; set; }
    public Guid StoreProductId { get; set; }
    public string StoreProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class CreateProductIngredientDto
{
    public Guid StoreProductId { get; set; }
    public decimal Quantity { get; set; }
}

public class BulkUpdateAvailabilityDto
{
    public List<Guid> ProductIds { get; set; } = new();
    public bool IsAvailable { get; set; }
}
