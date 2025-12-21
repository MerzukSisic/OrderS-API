using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class InventoryLog
{
    public Guid Id { get; set; }

    public Guid StoreProductId { get; set; }

    public int QuantityChange { get; set; }

    public string Type { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual StoreProduct StoreProduct { get; set; } = null!;
}
