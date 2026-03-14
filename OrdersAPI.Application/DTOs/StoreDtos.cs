using System.ComponentModel.DataAnnotations;

namespace OrdersAPI.Application.DTOs;

public class StoreDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public bool IsExternal { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProductsCount { get; set; }
}

public class CreateStoreDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    public bool IsExternal { get; set; } = false;
}

public class UpdateStoreDto
{
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    public bool? IsExternal { get; set; }
}