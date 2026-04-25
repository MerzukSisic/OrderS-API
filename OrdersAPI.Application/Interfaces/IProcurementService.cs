using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Application.Interfaces;

public interface IProcurementService
{
    Task<PagedResult<ProcurementOrderDto>> GetAllProcurementOrdersAsync(Guid? storeId = null, int page = 1, int pageSize = 50);
    Task<ProcurementOrderDto> GetProcurementOrderByIdAsync(Guid id);
    Task<ProcurementOrderDto> CreateProcurementOrderAsync(CreateProcurementDto dto);
    Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(Guid procurementOrderId);
    Task ConfirmPaymentAsync(Guid procurementOrderId, string paymentIntentId);
    Task UpdateProcurementStatusAsync(Guid procurementOrderId, ProcurementStatus status);
    Task ReceiveProcurementAsync(Guid procurementOrderId, ReceiveProcurementDto dto);
}
