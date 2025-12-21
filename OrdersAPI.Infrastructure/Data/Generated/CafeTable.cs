using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class CafeTable
{
    public Guid Id { get; set; }

    public string TableNumber { get; set; } = null!;

    public int Capacity { get; set; }

    public string Status { get; set; } = null!;

    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
