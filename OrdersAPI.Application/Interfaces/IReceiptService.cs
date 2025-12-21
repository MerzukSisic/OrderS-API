using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IReceiptService
{
    Task<ReceiptDto> GenerateCustomerReceiptAsync(Guid orderId);
    Task<KitchenReceiptDto> GenerateKitchenReceiptAsync(Guid orderId);
    Task<BarReceiptDto> GenerateBarReceiptAsync(Guid orderId);
}
