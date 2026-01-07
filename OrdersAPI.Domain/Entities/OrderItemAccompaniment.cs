namespace OrdersAPI.Domain.Entities;

public class OrderItemAccompaniment
{
    public Guid Id { get; set; }
    public Guid OrderItemId { get; set; }
    public Guid AccompanimentId { get; set; }
    public decimal PriceAtOrder { get; set; } // Cijena u trenutku naručivanja (u slučaju da se cijene mijenjaju)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrderItem OrderItem { get; set; } = null!;
    public Accompaniment Accompaniment { get; set; } = null!;
}