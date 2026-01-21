using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class ProcurementService : IProcurementService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProcurementService> _logger;
    private readonly IStripeService _stripeService;

    // ✅ UPDATED CONSTRUCTOR - Added IStripeService
    public ProcurementService(
        ApplicationDbContext context,
        ILogger<ProcurementService> logger,
        IStripeService stripeService)
    {
        _context = context;
        _logger = logger;
        _stripeService = stripeService;
    }

    public async Task<IEnumerable<ProcurementOrderDto>> GetAllProcurementOrdersAsync(Guid? storeId = null)
    {
        var query = _context.ProcurementOrders
            .AsNoTracking()
            .Include(p => p.Store)
            .Include(p => p.Items)
                .ThenInclude(i => i.StoreProduct)
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(p => p.StoreId == storeId);

        var orders = await query
            .OrderByDescending(p => p.OrderDate)
            .Select(p => new ProcurementOrderDto
            {
                Id = p.Id,
                StoreId = p.StoreId,
                StoreName = p.Store.Name,
                Supplier = p.Supplier,
                TotalAmount = p.TotalAmount,
                Status = p.Status.ToString(),
                StripePaymentIntentId = p.StripePaymentIntentId,
                Notes = p.Notes,
                OrderDate = p.OrderDate,
                DeliveryDate = p.DeliveryDate,
                Items = p.Items.Select(i => new ProcurementOrderItemDto
                {
                    Id = i.Id,
                    StoreProductId = i.StoreProductId,
                    StoreProductName = i.StoreProduct.Name,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    Subtotal = i.Subtotal
                }).ToList()
            })
            .ToListAsync();

        return orders;
    }

    public async Task<ProcurementOrderDto> GetProcurementOrderByIdAsync(Guid id)
    {
        var order = await _context.ProcurementOrders
            .AsNoTracking()
            .Include(p => p.Store)
            .Include(p => p.Items)
                .ThenInclude(i => i.StoreProduct)
            .Select(p => new ProcurementOrderDto
            {
                Id = p.Id,
                StoreId = p.StoreId,
                StoreName = p.Store.Name,
                Supplier = p.Supplier,
                TotalAmount = p.TotalAmount,
                Status = p.Status.ToString(),
                StripePaymentIntentId = p.StripePaymentIntentId,
                Notes = p.Notes,
                OrderDate = p.OrderDate,
                DeliveryDate = p.DeliveryDate,
                Items = p.Items.Select(i => new ProcurementOrderItemDto
                {
                    Id = i.Id,
                    StoreProductId = i.StoreProductId,
                    StoreProductName = i.StoreProduct.Name,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    Subtotal = i.Subtotal
                }).ToList()
            })
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
            throw new KeyNotFoundException($"Procurement order with ID {id} not found");

        return order;
    }

    public async Task<ProcurementOrderDto> CreateProcurementOrderAsync(CreateProcurementDto dto)
    {
        var storeExists = await _context.Stores.AnyAsync(s => s.Id == dto.StoreId);
        if (!storeExists)
            throw new KeyNotFoundException($"Store with ID {dto.StoreId} not found");

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
                throw new KeyNotFoundException($"Store product with ID {itemDto.StoreProductId} not found");

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

        _logger.LogInformation("Procurement order {OrderId} created for store {StoreId} with {ItemCount} items", 
            procurement.Id, dto.StoreId, procurement.Items.Count);

        return await GetProcurementOrderByIdAsync(procurement.Id);
    }

    // ✅ UPDATED - Uses real Stripe integration
    public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(Guid procurementOrderId)
    {
        var order = await _context.ProcurementOrders.FindAsync(procurementOrderId);
        if (order == null)
            throw new KeyNotFoundException($"Procurement order with ID {procurementOrderId} not found");

        // Create payment intent via Stripe
        var createDto = new CreatePaymentIntentDto
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Currency = "bam", // Will be converted to EUR by StripeService
            TableNumber = $"Procurement-{order.Supplier}"
        };

        var paymentIntent = await _stripeService.CreatePaymentIntentAsync(createDto);

        _logger.LogInformation(
            "Stripe Payment Intent created for procurement order {OrderId}: {PaymentIntentId}, Amount: {Amount} {Currency}", 
            order.Id, paymentIntent.PaymentIntentId, paymentIntent.Amount, paymentIntent.Currency);

        return paymentIntent;
    }

    // ✅ UPDATED - Verifies payment with Stripe
    public async Task ConfirmPaymentAsync(Guid procurementOrderId, string paymentIntentId)
    {
        var order = await _context.ProcurementOrders
            .FirstOrDefaultAsync(o => o.Id == procurementOrderId);

        if (order == null)
            throw new InvalidOperationException("Procurement order not found");

        // ⚠️ TEST MODE: Auto-approve payments in development
#if DEBUG
        _logger.LogWarning($"⚠️ TEST MODE: Auto-approving payment {paymentIntentId} for order {procurementOrderId}");
        order.Status = ProcurementStatus.Paid;
        order.StripePaymentIntentId = paymentIntentId;
        await _context.SaveChangesAsync();
    
        _logger.LogInformation($"✅ Payment {paymentIntentId} auto-approved for order {procurementOrderId}");
        return;
#endif

         //PRODUCTION: Verify with Stripe
        var paymentIntent = await _stripeService.GetPaymentIntentAsync(paymentIntentId);
    
        if (paymentIntent.Status != "succeeded")
        {
            _logger.LogError($"❌ Payment intent {paymentIntentId} status: {paymentIntent.Status}");
            throw new InvalidOperationException($"Payment failed or not confirmed with Stripe. Status: {paymentIntent.Status}");
        }
    
        order.Status = ProcurementStatus.Paid;
        order.StripePaymentIntentId = paymentIntentId;
        await _context.SaveChangesAsync();
    
        _logger.LogInformation($"✅ Payment confirmed for procurement order {procurementOrderId}");
    }

    public async Task UpdateProcurementStatusAsync(Guid id, ProcurementStatus status)
    {
        var order = await _context.ProcurementOrders.FindAsync(id);
        if (order == null)
            throw new KeyNotFoundException($"Procurement order with ID {id} not found");

        order.Status = status;

        if (status == ProcurementStatus.Received)
            order.DeliveryDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Procurement order {OrderId} status updated to {Status}", id, status);
    }

    public async Task ReceiveProcurementAsync(Guid procurementOrderId, ReceiveProcurementDto dto)
    {
        var order = await _context.ProcurementOrders
            .Include(p => p.Items)
                .ThenInclude(i => i.StoreProduct)
            .FirstOrDefaultAsync(p => p.Id == procurementOrderId);

        if (order == null)
            throw new KeyNotFoundException($"Procurement order with ID {procurementOrderId} not found");

        if (order.Status != ProcurementStatus.Paid)
            throw new InvalidOperationException("Order must be paid before receiving");

        foreach (var receivedItem in dto.Items)
        {
            var orderItem = order.Items.FirstOrDefault(i => i.Id == receivedItem.ItemId);
            if (orderItem == null)
                throw new KeyNotFoundException($"Order item with ID {receivedItem.ItemId} not found");

            if (receivedItem.ReceivedQuantity > orderItem.Quantity)
                throw new InvalidOperationException(
                    $"Received quantity ({receivedItem.ReceivedQuantity}) cannot exceed ordered quantity ({orderItem.Quantity})"
                );

            // Update stock
            orderItem.StoreProduct.CurrentStock += receivedItem.ReceivedQuantity;
            orderItem.StoreProduct.LastRestocked = DateTime.UtcNow;

            // Log inventory change
            _context.InventoryLogs.Add(new InventoryLog
            {
                Id = Guid.NewGuid(),
                StoreProductId = orderItem.StoreProductId,
                QuantityChange = receivedItem.ReceivedQuantity,
                Type = InventoryLogType.Restock,
                Reason = $"Procurement Order {order.Id} - Received {receivedItem.ReceivedQuantity}/{orderItem.Quantity}",
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Received {ReceivedQty}/{OrderedQty} of {ProductName} for procurement {OrderId}",
                receivedItem.ReceivedQuantity, orderItem.Quantity, orderItem.StoreProduct.Name, procurementOrderId);
        }

        // Update order status
        order.Status = ProcurementStatus.Received;
        order.DeliveryDate = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.Notes))
            order.Notes = $"{order.Notes}\n[RECEIVED: {dto.Notes}]";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Procurement order {OrderId} fully received and inventory updated", procurementOrderId);
    }
}