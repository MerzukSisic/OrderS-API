using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class ProcurementOrderItem
{
    public Guid Id { get; set; }

    public Guid ProcurementOrderId { get; set; }

    public Guid StoreProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal Subtotal { get; set; }

    public virtual ProcurementOrder ProcurementOrder { get; set; } = null!;

    public virtual StoreProduct StoreProduct { get; set; } = null!;
}
