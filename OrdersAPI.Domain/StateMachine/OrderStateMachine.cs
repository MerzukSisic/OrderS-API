using OrdersAPI.Domain.Enums;
using OrdersAPI.Domain.Exceptions;

namespace OrdersAPI.Domain.StateMachine;

public static class OrderStateMachine
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ValidTransitions = new()
    {
        [OrderStatus.Pending]    = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing]  = [OrderStatus.Ready, OrderStatus.Cancelled],
        [OrderStatus.Ready]      = [OrderStatus.Completed, OrderStatus.Cancelled],
        [OrderStatus.Completed]  = [],
        [OrderStatus.Cancelled]  = [],
    };

    private static readonly Dictionary<OrderItemStatus, HashSet<OrderItemStatus>> ValidItemTransitions = new()
    {
        [OrderItemStatus.Pending]   = [OrderItemStatus.Preparing, OrderItemStatus.Cancelled],
        [OrderItemStatus.Preparing] = [OrderItemStatus.Ready, OrderItemStatus.Cancelled],
        [OrderItemStatus.Ready]     = [OrderItemStatus.Completed, OrderItemStatus.Cancelled],
        [OrderItemStatus.Completed] = [],
        [OrderItemStatus.Cancelled] = [],
    };

    public static void ValidateTransition(OrderStatus from, OrderStatus to)
    {
        if (!ValidTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
            throw new BusinessException(
                $"Invalid order status transition from '{from}' to '{to}'. " +
                $"Allowed transitions from '{from}': [{string.Join(", ", ValidTransitions.GetValueOrDefault(from, []))}]");
    }

    public static void ValidateItemTransition(OrderItemStatus from, OrderItemStatus to)
    {
        if (!ValidItemTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
            throw new BusinessException(
                $"Invalid order item status transition from '{from}' to '{to}'. " +
                $"Allowed transitions from '{from}': [{string.Join(", ", ValidItemTransitions.GetValueOrDefault(from, []))}]");
    }

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static bool CanItemTransition(OrderItemStatus from, OrderItemStatus to) =>
        ValidItemTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
