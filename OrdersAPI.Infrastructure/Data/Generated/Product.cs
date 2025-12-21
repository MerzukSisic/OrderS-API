using System;
using System.Collections.Generic;

namespace OrdersAPI.Infrastructure.Data.Generated;

public partial class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public Guid CategoryId { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }

    public string Location { get; set; } = null!;

    public int PreparationTimeMinutes { get; set; }

    public int Stock { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
}
