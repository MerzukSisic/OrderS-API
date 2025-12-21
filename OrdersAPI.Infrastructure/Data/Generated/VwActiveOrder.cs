using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class VwActiveOrder
{
    public Guid OrderId { get; set; }

    public string Status { get; set; } = null!;

    public string Type { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public string WaiterName { get; set; } = null!;

    public string? TableNumber { get; set; }

    public int? ItemCount { get; set; }
}
