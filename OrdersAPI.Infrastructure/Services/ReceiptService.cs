using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class ReceiptService(ApplicationDbContext context, IMapper mapper, ILogger<ReceiptService> logger)
    : IReceiptService
{
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<ReceiptService> _logger = logger;

    public async Task<ReceiptDto> GenerateCustomerReceiptAsync(Guid orderId)
    {
        var order = await context.Orders
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order {orderId} not found");

        var receipt = new ReceiptDto
        {
            OrderId = order.Id,
            OrderNumber = order.Id.ToString().Substring(0, 8),
            TableNumber = order.Table?.TableNumber,
            WaiterName = order.Waiter.FullName,
            CreatedAt = order.CreatedAt,
            OrderType = order.Type.ToString(),
            DeliveryMethod = order.Type == OrderType.DineIn ? "In store" : "Takeout",
            Items = order.Items.Select(i => new ReceiptItemDto
            {
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                Notes = i.Notes
            }).ToList(),
            Subtotal = order.TotalAmount,
            Tax = 0, // PDV calculacija ako treba
            Total = order.TotalAmount
        };

        return receipt;
    }

    public async Task<KitchenReceiptDto> GenerateKitchenReceiptAsync(Guid orderId)
    {
        var order = await context.Orders
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.ProductIngredients)
                        .ThenInclude(pi => pi.StoreProduct)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order {orderId} not found");

        var kitchenItems = order.Items
            .Where(i => i.Product.Location == PreparationLocation.Kitchen)
            .Select(i => new KitchenReceiptItemDto
            {
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                Notes = i.Notes,
                Ingredients = i.Product.ProductIngredients
                    .Select(pi => pi.StoreProduct.Name)
                    .ToList()
            }).ToList();

        return new KitchenReceiptDto
        {
            OrderId = order.Id,
            TableNumber = order.Table?.TableNumber,
            WaiterName = order.Waiter.FullName,
            CreatedAt = order.CreatedAt,
            OrderType = order.Type.ToString(),
            DeliveryMethod = order.Type == OrderType.DineIn ? "In store" : "Takeout",
            Items = kitchenItems
        };
    }

    public async Task<BarReceiptDto> GenerateBarReceiptAsync(Guid orderId)
    {
        var order = await context.Orders
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order {orderId} not found");

        var barItems = order.Items
            .Where(i => i.Product.Location == PreparationLocation.Bar)
            .Select(i => new BarReceiptItemDto
            {
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                Notes = i.Notes
            }).ToList();

        return new BarReceiptDto
        {
            OrderId = order.Id,
            TableNumber = order.Table?.TableNumber,
            WaiterName = order.Waiter.FullName,
            CreatedAt = order.CreatedAt,
            Items = barItems
        };
    }
}

