using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class ProcurementService(
    ApplicationDbContext context,
    IMapper mapper,
    IStripeService stripeService,
    ILogger<ProcurementService> logger)
    : IProcurementService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IStripeService _stripeService = stripeService;

    public async Task<IEnumerable<ProcurementOrderDto>> GetAllProcurementOrdersAsync(Guid? storeId = null)
    {
        var query = _context.ProcurementOrders
            .Include(p => p.Store)
            .Include(p => p.Items)
                .ThenInclude(i => i.StoreProduct)
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(p => p.StoreId == storeId);

        var orders = await query
            .OrderByDescending(p => p.OrderDate)
            .ToListAsync();

        return mapper.Map<IEnumerable<ProcurementOrderDto>>(orders);
    }

    public async Task<ProcurementOrderDto> GetProcurementOrderByIdAsync(Guid id)
    {
        var order = await _context.ProcurementOrders
            .Include(p => p.Store)
            .Include(p => p.Items)
                .ThenInclude(i => i.StoreProduct)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
            throw new KeyNotFoundException($"Procurement order {id} not found");

        return mapper.Map<ProcurementOrderDto>(order);
    }

    public async Task<ProcurementOrderDto> CreateProcurementOrderAsync(CreateProcurementDto dto)
    {
        var procurement = new ProcurementOrder
        {
            Id = Guid.NewGuid(),
            StoreId = dto.StoreId,
            Supplier = dto.Supplier,
            Status = ProcurementStatus.Pending,
            OrderDate = DateTime.UtcNow,
            Notes = dto.Notes
        };

        decimal totalAmount = 0;

        foreach (var itemDto in dto.Items)
        {
            var storeProduct = await _context.StoreProducts.FindAsync(itemDto.StoreProductId);
            if (storeProduct == null)
                throw new KeyNotFoundException($"Store product {itemDto.StoreProductId} not found");

            var subtotal = storeProduct.PurchasePrice * itemDto.Quantity;
            totalAmount += subtotal;

            procurement.Items.Add(new ProcurementOrderItem
            {
                Id = Guid.NewGuid(),
                StoreProductId = itemDto.StoreProductId,
                Quantity = itemDto.Quantity,
                UnitCost = storeProduct.PurchasePrice,
                Subtotal = subtotal
            });
        }

        procurement.TotalAmount = totalAmount;
        _context.ProcurementOrders.Add(procurement);
        await _context.SaveChangesAsync();

        logger.LogInformation("Procurement order {OrderId} created", procurement.Id);

        return await GetProcurementOrderByIdAsync(procurement.Id);
    }

    public async Task<string> CreatePaymentIntentAsync(Guid procurementOrderId)
    {
        var order = await _context.ProcurementOrders.FindAsync(procurementOrderId);
        if (order == null)
            throw new KeyNotFoundException($"Procurement order {procurementOrderId} not found");

        // Stripe Payment Intent
        var clientSecret = await _stripeService.CreatePaymentIntentAsync(order.TotalAmount);

        logger.LogInformation("Payment intent created for procurement order {OrderId}", procurementOrderId);

        return clientSecret;
    }

    public async Task ConfirmPaymentAsync(Guid procurementOrderId, string paymentIntentId)
    {
        var order = await _context.ProcurementOrders
            .Include(p => p.Items)
                .ThenInclude(i => i.StoreProduct)
            .FirstOrDefaultAsync(p => p.Id == procurementOrderId);

        if (order == null)
            throw new KeyNotFoundException($"Procurement order {procurementOrderId} not found");

        var paymentSucceeded = await _stripeService.ConfirmPaymentAsync(paymentIntentId);

        if (!paymentSucceeded)
            throw new InvalidOperationException("Payment failed");

        // Update status
        order.Status = ProcurementStatus.Paid;
        order.StripePaymentIntentId = paymentIntentId;

        // Update inventory - RESTOCK
        foreach (var item in order.Items)
        {
            item.StoreProduct.CurrentStock += item.Quantity;
            item.StoreProduct.LastRestocked = DateTime.UtcNow;

            _context.InventoryLogs.Add(new InventoryLog
            {
                Id = Guid.NewGuid(),
                StoreProductId = item.StoreProductId,
                QuantityChange = item.Quantity,
                Type = InventoryLogType.Restock,
                Reason = $"Procurement Order {order.Id}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        logger.LogInformation("Procurement order {OrderId} paid and inventory updated", procurementOrderId);
    }

    public async Task UpdateProcurementStatusAsync(Guid id, ProcurementStatus status)
    {
        var order = await _context.ProcurementOrders.FindAsync(id);
        if (order == null)
            throw new KeyNotFoundException($"Procurement order {id} not found");

        order.Status = status;

        if (status == ProcurementStatus.Received)
            order.DeliveryDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        logger.LogInformation("Procurement order {OrderId} status updated to {Status}", id, status);
    }
}

