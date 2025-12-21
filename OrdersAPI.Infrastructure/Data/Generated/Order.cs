using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class Order
{
    public Guid Id { get; set; }

    public Guid WaiterId { get; set; }

    public Guid? TableId { get; set; }

    public string Status { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsPartnerOrder { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual CafeTable? Table { get; set; }

    public virtual User Waiter { get; set; } = null!;
}
