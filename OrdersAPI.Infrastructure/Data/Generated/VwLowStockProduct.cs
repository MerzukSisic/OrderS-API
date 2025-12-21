using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class VwLowStockProduct
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public int CurrentStock { get; set; }

    public int MinimumStock { get; set; }

    public string Unit { get; set; } = null!;

    public string StoreName { get; set; } = null!;
}
