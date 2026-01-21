using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class ReceiptService(ApplicationDbContext context, ILogger<ReceiptService> logger) : IReceiptService
{
    private const decimal TAX_RATE = 0.17m; // PDV 17% u BiH
    private const decimal PARTNER_DISCOUNT = 0.10m; // 10% popust za partner orders

    public async Task<ReceiptDto> GenerateCustomerReceiptAsync(Guid orderId)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.OrderItemAccompaniments)
                    .ThenInclude(oia => oia.Accompaniment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order with ID {orderId} not found");

        var subtotal = order.Items.Sum(i => i.Subtotal);
        var discount = order.IsPartnerOrder ? subtotal * PARTNER_DISCOUNT : 0;
        var taxableAmount = subtotal - discount;
        var tax = taxableAmount * TAX_RATE;
        var total = taxableAmount + tax;

        var receipt = new ReceiptDto
        {
            OrderId = order.Id,
            OrderNumber = FormatOrderNumber(order.Id, order.CreatedAt),
            TableNumber = order.Table?.TableNumber,
            WaiterName = order.Waiter.FullName,
            CreatedAt = order.CreatedAt,
            CompletedAt = order.CompletedAt,
            OrderType = order.Type.ToString(),
            Status = order.Status.ToString(),
            IsPartnerOrder = order.IsPartnerOrder,
            Items = order.Items.Select(i => new ReceiptItemDto
            {
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                Notes = i.Notes,
                SelectedAccompaniments = i.OrderItemAccompaniments
                    .Select(oia => oia.Accompaniment.Name)
                    .ToList()
            }).ToList(),
            Subtotal = subtotal,
            Tax = tax,
            Discount = discount,
            Total = total,
            Notes = order.Notes
        };

        logger.LogInformation("Generated customer receipt for order {OrderId}", orderId);

        return receipt;
    }

    public async Task<KitchenReceiptDto> GenerateKitchenReceiptAsync(Guid orderId)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.ProductIngredients)
                        .ThenInclude(pi => pi.StoreProduct)
            .Include(o => o.Items)
                .ThenInclude(i => i.OrderItemAccompaniments)
                    .ThenInclude(oia => oia.Accompaniment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order with ID {orderId} not found");

        var kitchenItems = order.Items
            .Where(i => i.Product.Location == PreparationLocation.Kitchen)
            .Select(i => new KitchenReceiptItemDto
            {
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                Notes = i.Notes,
                SelectedAccompaniments = i.OrderItemAccompaniments
                    .Select(oia => oia.Accompaniment.Name)
                    .ToList(),
                Ingredients = i.Product.ProductIngredients
                    .Select(pi => $"{pi.StoreProduct.Name} ({pi.Quantity} {pi.StoreProduct.Unit})")
                    .ToList()
            }).ToList();

        var receipt = new KitchenReceiptDto
        {
            OrderId = order.Id,
            OrderNumber = FormatOrderNumber(order.Id, order.CreatedAt),
            TableNumber = order.Table?.TableNumber,
            WaiterName = order.Waiter.FullName,
            CreatedAt = order.CreatedAt,
            OrderType = order.Type.ToString(),
            Items = kitchenItems
        };

        logger.LogInformation("Generated kitchen receipt for order {OrderId} with {ItemCount} items", 
            orderId, kitchenItems.Count);

        return receipt;
    }

    public async Task<BarReceiptDto> GenerateBarReceiptAsync(Guid orderId)
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Waiter)
            .Include(o => o.Table)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.OrderItemAccompaniments)
                    .ThenInclude(oia => oia.Accompaniment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException($"Order with ID {orderId} not found");

        var barItems = order.Items
            .Where(i => i.Product.Location == PreparationLocation.Bar)
            .Select(i => new BarReceiptItemDto
            {
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                Notes = i.Notes,
                SelectedAccompaniments = i.OrderItemAccompaniments
                    .Select(oia => oia.Accompaniment.Name)
                    .ToList()
            }).ToList();

        var receipt = new BarReceiptDto
        {
            OrderId = order.Id,
            OrderNumber = FormatOrderNumber(order.Id, order.CreatedAt),
            TableNumber = order.Table?.TableNumber,
            WaiterName = order.Waiter.FullName,
            CreatedAt = order.CreatedAt,
            OrderType = order.Type.ToString(),
            Items = barItems
        };

        logger.LogInformation("Generated bar receipt for order {OrderId} with {ItemCount} items", 
            orderId, barItems.Count);

        return receipt;
    }

    private static string FormatOrderNumber(Guid orderId, DateTime createdAt)
    {
        // Format: YYMMDD-XXXX (datum + prvih 4 karaktera GUID-a)
        return $"{createdAt:yyMMdd}-{orderId.ToString().Substring(0, 4).ToUpper()}";
    }
}
