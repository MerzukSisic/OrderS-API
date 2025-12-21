namespace OrdersAPI.Domain.Entities;

public class ProcurementOrderItem
{
    public Guid Id { get; set; }
    public Guid ProcurementOrderId { get; set; }
    public Guid StoreProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }

    public ProcurementOrder ProcurementOrder { get; set; } = null!;
    public StoreProduct StoreProduct { get; set; } = null!;
}
