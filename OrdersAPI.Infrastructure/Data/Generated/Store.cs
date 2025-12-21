using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class Store
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ProcurementOrder> ProcurementOrders { get; set; } = new List<ProcurementOrder>();

    public virtual ICollection<StoreProduct> StoreProducts { get; set; } = new List<StoreProduct>();
}
