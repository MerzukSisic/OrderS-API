namespace OrdersAPI.Domain.Entities;

public enum OrderStatus
{
    Pending,
    Preparing,
    Ready,
    Completed,
    Cancelled
}
