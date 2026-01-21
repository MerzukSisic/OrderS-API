using System.ComponentModel.DataAnnotations;

namespace OrdersAPI.Application.DTOs;

public class StoreDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalProducts { get; set; } // Broj StoreProduct-a u store-u
    public int LowStockProductsCount { get; set; } // Proizvodi sa low stock
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
}

public class UpdateStoreDto
{
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }
}