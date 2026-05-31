using OrdersAPI.Domain.Enums;
using OrdersAPI.Domain.Exceptions;

namespace OrdersAPI.Domain.StateMachine;

public static class ProcurementStateMachine
{
    private static readonly Dictionary<ProcurementStatus, HashSet<ProcurementStatus>> ValidTransitions = new()
    {
        [ProcurementStatus.Pending]   = [ProcurementStatus.Ordered, ProcurementStatus.Cancelled],
        [ProcurementStatus.Paid]      = [ProcurementStatus.Ordered, ProcurementStatus.Cancelled],
        [ProcurementStatus.Ordered]   = [ProcurementStatus.Received, ProcurementStatus.Cancelled],
        [ProcurementStatus.Received]  = [],
        [ProcurementStatus.Cancelled] = [],
    };

    public static void ValidateTransition(ProcurementStatus from, ProcurementStatus to)
    {
        if (!ValidTransitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
            throw new BusinessException(
                $"Invalid procurement status transition from '{from}' to '{to}'. " +
                $"Allowed transitions from '{from}': [{string.Join(", ", ValidTransitions.GetValueOrDefault(from, []))}]");
    }

    public static bool CanTransition(ProcurementStatus from, ProcurementStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
