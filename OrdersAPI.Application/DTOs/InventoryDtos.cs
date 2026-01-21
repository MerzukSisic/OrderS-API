namespace OrdersAPI.Application.DTOs;

public class StoreProductDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public int CurrentStock { get; set; }
    public int MinimumStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsLowStock { get; set; }
    public DateTime LastRestocked { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateStoreProductDto
{
    public Guid StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PurchasePrice { get; set; }
    public int CurrentStock { get; set; } = 0;
    public int MinimumStock { get; set; } = 10;
    public string Unit { get; set; } = "pcs";
}

public class UpdateStoreProductDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? PurchasePrice { get; set; }
    public int? CurrentStock { get; set; }
    public int? MinimumStock { get; set; }
    public string? Unit { get; set; }
}

public class InventoryLogDto
{
    public Guid Id { get; set; }
    public Guid StoreProductId { get; set; }
    public string StoreProductName { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdjustInventoryDto
{
    public int QuantityChange { get; set; }
    public string Type { get; set; } = "Adjustment";
    public string? Reason { get; set; }
}

public class ConsumptionForecastDto
{
    public Guid StoreProductId { get; set; }
    public string StoreProductName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public double AverageDailyConsumption { get; set; }
    public int EstimatedDaysUntilDepletion { get; set; }
    public bool NeedsReorder { get; set; }
    public string Unit { get; set; } = string.Empty;
}
