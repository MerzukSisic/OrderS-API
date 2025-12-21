using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;

namespace OrdersAPI.Application.Interfaces;

public interface IProcurementService
{
    Task<IEnumerable<ProcurementOrderDto>> GetAllProcurementOrdersAsync(Guid? storeId = null);
    Task<ProcurementOrderDto> GetProcurementOrderByIdAsync(Guid id);
    Task<ProcurementOrderDto> CreateProcurementOrderAsync(CreateProcurementDto dto);
    Task<string> CreatePaymentIntentAsync(Guid procurementOrderId);
    Task ConfirmPaymentAsync(Guid procurementOrderId, string paymentIntentId);
    Task UpdateProcurementStatusAsync(Guid procurementOrderId, ProcurementStatus status);
}
