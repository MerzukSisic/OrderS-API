using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class ProductIngredient
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public Guid StoreProductId { get; set; }

    public decimal Quantity { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual StoreProduct StoreProduct { get; set; } = null!;
}
