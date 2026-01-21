namespace OrdersAPI.Domain.Entities;

public class ProductIngredient
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid StoreProductId { get; set; }
    public decimal Quantity { get; set; }

    public Product Product { get; set; } = null!;
    public StoreProduct StoreProduct { get; set; } = null!;
}