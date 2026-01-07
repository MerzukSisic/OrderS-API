namespace OrdersAPI.Domain.Enums;

public enum OrderItemStatus
{
    Pending,
    Preparing,
    Ready,
    Completed,   // ✅ DODATO
    Cancelled    // ✅ DODATO
}