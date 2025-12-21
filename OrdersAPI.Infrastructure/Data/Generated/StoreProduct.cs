using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class StoreProduct
{
    public Guid Id { get; set; }

    public Guid StoreId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal PurchasePrice { get; set; }

    public int CurrentStock { get; set; }

    public int MinimumStock { get; set; }

    public string Unit { get; set; } = null!;

    public DateTime LastRestocked { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<InventoryLog> InventoryLogs { get; set; } = new List<InventoryLog>();

    public virtual ICollection<ProcurementOrderItem> ProcurementOrderItems { get; set; } = new List<ProcurementOrderItem>();

    public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();

    public virtual Store Store { get; set; } = null!;
}
