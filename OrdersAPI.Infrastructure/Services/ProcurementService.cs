using OrdersAPI.Domain.Exceptions;
using OrdersAPI.Domain.StateMachine;
﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public ProcurementService(
        ApplicationDbContext context,
        ILogger<ProcurementService> logger,
        IStripeService stripeService,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _stripeService = stripeService;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<PagedResult<ProcurementOrderDto>> GetAllProcurementOrdersAsync(Guid? storeId = null, int page = 1, int pageSize = 50)
    {
        var clampedPageSize = Math.Min(pageSize, 100);
        var query = _context.ProcurementOrders
            .AsNoTracking()
            .Include(p => p.Store)
            .Include(p => p.SourceStore)
            .Include(p => p.Items)
                .ThenInclude(i => i.StoreProduct)
            .AsQueryable();

        if (storeId.HasValue)
            query = query.Where(p => p.StoreId == storeId);

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(p => p.OrderDate)
            .Skip((page - 1) * clampedPageSize)
            .Take(clampedPageSize)
            .Select(p => new ProcurementOrderDto
            {
                Id = p.Id,
                StoreId = p.StoreId,
                StoreName = p.Store.Name,
                SourceStoreId = p.SourceStoreId,
                SourceStoreName = p.SourceStore != null ? p.SourceStore.Name : null,
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
                    StoreProductName = i.StoreProduct != null ? i.StoreProduct.Name : string.Empty,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    Subtotal = i.Subtotal
                }).ToList()
            })
            .ToListAsync();

        return new PagedResult<ProcurementOrderDto> { Items = orders, TotalCount = totalCount, Page = page, PageSize = clampedPageSize };
    }

    public async Task<ProcurementOrderDto> GetProcurementOrderByIdAsync(Guid id)
    {
        var order = await _context.ProcurementOrders
            .AsNoTracking()
            .Include(p => p.Store)
            .Include(p => p.SourceStore)
            .Include(p => p.Items)
                .ThenInclude(i => i.StoreProduct)
            .Select(p => new ProcurementOrderDto
            {
                Id = p.Id,
                StoreId = p.StoreId,
                StoreName = p.Store.Name,
                SourceStoreId = p.SourceStoreId,
                SourceStoreName = p.SourceStore != null ? p.SourceStore.Name : null,
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
                    StoreProductName = i.StoreProduct != null ? i.StoreProduct.Name : string.Empty,
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost,
                    Subtotal = i.Subtotal
                }).ToList()
            })
            .FirstOrDefaultAsync(p => p.Id == id);

        if (order == null)
            throw new NotFoundException($"Procurement order with ID {id} not found");

        return order;
    }

    public async Task<ProcurementOrderDto> CreateProcurementOrderAsync(CreateProcurementDto dto)
    {
        var storeExists = await _context.Stores.AnyAsync(s => s.Id == dto.StoreId);
        if (!storeExists)
            throw new NotFoundException($"Store with ID {dto.StoreId} not found");

        bool sourceStoreIsInternal = false;
        if (dto.SourceStoreId.HasValue)
        {
            var sourceStore = await _context.Stores.FindAsync(dto.SourceStoreId.Value);
            if (sourceStore == null)
                throw new NotFoundException($"Source store with ID {dto.SourceStoreId} not found");
            sourceStoreIsInternal = !sourceStore.IsExternal;
        }

        var procurement = new ProcurementOrder
        {
            Id = Guid.NewGuid(),
            StoreId = dto.StoreId,
            SourceStoreId = dto.SourceStoreId,
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
                throw new NotFoundException($"Store product with ID {itemDto.StoreProductId} not found");

            if (sourceStoreIsInternal)
            {
                // Internal source: check stock now, deduct when received
                if (storeProduct.CurrentStock < itemDto.Quantity)
                    throw new BusinessException(
                        $"Insufficient stock for '{storeProduct.Name}'. Available: {storeProduct.CurrentStock}, Requested: {itemDto.Quantity}");
            }
            else if (dto.SourceStoreId.HasValue)
            {
                // External source: deduct stock immediately on order creation
                if (storeProduct.CurrentStock < itemDto.Quantity)
                    throw new BusinessException(
                        $"Insufficient stock for '{storeProduct.Name}'. Available: {storeProduct.CurrentStock}, Requested: {itemDto.Quantity}");

                storeProduct.CurrentStock -= itemDto.Quantity;

                _context.InventoryLogs.Add(new InventoryLog
                {
                    Id = Guid.NewGuid(),
                    StoreProductId = storeProduct.Id,
                    QuantityChange = -itemDto.Quantity,
                    Type = InventoryLogType.Sale,
                    Reason = $"Procurement order placed to store {dto.StoreId} (Order ID: {procurement.Id})",
                    CreatedAt = DateTime.UtcNow
                });
            }

            var unitCost = itemDto.UnitCost ?? storeProduct.PurchasePrice;
            var subtotal = unitCost * itemDto.Quantity;
            totalAmount += subtotal;

            procurement.Items.Add(new ProcurementOrderItem
            {
                Id = Guid.NewGuid(),
                StoreProductId = itemDto.StoreProductId,
                Quantity = itemDto.Quantity,
                UnitCost = unitCost,
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

    public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(Guid procurementOrderId)
    {
        var order = await _context.ProcurementOrders.FindAsync(procurementOrderId);
        if (order == null)
            throw new NotFoundException($"Procurement order with ID {procurementOrderId} not found");

        if (order.Status != ProcurementStatus.Pending)
            throw new BusinessException($"Cannot create payment for order with status '{order.Status}'. Order must be in Pending status.");

        // Fix 12: Provjeri postoji li već aktivan payment intent za ovu narudžbu
        if (!string.IsNullOrEmpty(order.StripePaymentIntentId))
        {
            try
            {
                var existingIntent = await _stripeService.GetPaymentIntentAsync(order.StripePaymentIntentId);
                if (existingIntent.Status is "requires_payment_method" or "requires_confirmation" or "requires_action")
                {
                    _logger.LogInformation("Returning existing payment intent {IntentId} for order {OrderId}", order.StripePaymentIntentId, order.Id);
                    return existingIntent;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve existing intent {IntentId}, creating new one", order.StripePaymentIntentId);
            }
        }

        // Amount se čita iz baze - klijent ne može manipulirati iznosom
        var createDto = new CreatePaymentIntentDto
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Currency = "bam",
            TableNumber = $"Procurement-{order.Supplier}"
        };

        var paymentIntent = await _stripeService.CreatePaymentIntentAsync(createDto);
        order.StripePaymentIntentId = paymentIntent.PaymentIntentId;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Stripe Payment Intent created for procurement order {OrderId}: {PaymentIntentId}, Amount: {Amount} {Currency}",
            order.Id, paymentIntent.PaymentIntentId, paymentIntent.Amount, paymentIntent.Currency);

        return paymentIntent;
    }

    public async Task ConfirmPaymentAsync(Guid procurementOrderId, string paymentIntentId)
    {
        var order = await _context.ProcurementOrders
            .Include(o => o.Items)
                .ThenInclude(i => i.StoreProduct)
            .FirstOrDefaultAsync(o => o.Id == procurementOrderId);

        if (order == null)
            throw new BusinessException("Procurement order not found");

        // Fix 13: Bypass SAMO u Development okruženju
        var bypassEnabled =
            bool.TryParse(_configuration["Stripe:BypassVerification"], out var parsedBypass) && parsedBypass;
        var bypassVerification = bypassEnabled && _environment.IsDevelopment();

        if (bypassEnabled && !_environment.IsDevelopment())
            _logger.LogWarning("Stripe:BypassVerification=true je ignorisan jer okruženje nije Development. Koristimo pravu Stripe provjeru.");

        if (bypassVerification)
        {
            _logger.LogWarning(
                "[DEV ONLY] Stripe:BypassVerification=true — auto-approving payment {PaymentIntentId} for order {OrderId}",
                paymentIntentId, procurementOrderId);
            order.Status = ProcurementStatus.Paid;
            order.StripePaymentIntentId = paymentIntentId;
            await _context.SaveChangesAsync();
            return;
        }

        // Default: verify with Stripe
        var paymentIntent = await _stripeService.GetPaymentIntentAsync(paymentIntentId);

        if (paymentIntent.Status != "succeeded")
        {
            _logger.LogError("Payment intent {PaymentIntentId} status: {Status}", paymentIntentId, paymentIntent.Status);
            throw new BusinessException($"Payment not confirmed by Stripe. Status: {paymentIntent.Status}");
        }

        order.Status = ProcurementStatus.Paid;
        order.StripePaymentIntentId = paymentIntentId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Payment confirmed for procurement order {OrderId}", procurementOrderId);
    }

    public async Task UpdateProcurementStatusAsync(Guid id, ProcurementStatus status)
    {
        var order = await _context.ProcurementOrders.FindAsync(id);
        if (order == null)
            throw new NotFoundException($"Procurement order with ID {id} not found");

        // Fix 15: Kritični statusi ne smiju biti postavljeni direktno - koristite dedicirane endpoint-e
        if (status == ProcurementStatus.Paid)
            throw new BusinessException("Status 'Paid' ne može biti postavljen direktno. Koristite payment endpoint (/payment-intent + /confirm-payment) koji verificira Stripe plaćanje.");

        if (status == ProcurementStatus.Received)
            throw new BusinessException("Status 'Received' ne može biti postavljen direktno. Koristite /receive endpoint koji provjerava i ažurira inventar.");

        // Fix 9: State machine validacija tranzicije statusa
        ProcurementStateMachine.ValidateTransition(order.Status, status);

        order.Status = status;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Procurement order {OrderId} status updated to {Status}", id, status);
    }

    public async Task ReceiveProcurementAsync(Guid procurementOrderId, ReceiveProcurementDto dto)
    {
        var order = await _context.ProcurementOrders
            .Include(p => p.Items)
                .ThenInclude(i => i.StoreProduct)
            .Include(p => p.SourceStore)
            .FirstOrDefaultAsync(p => p.Id == procurementOrderId);

        if (order == null)
            throw new NotFoundException($"Procurement order with ID {procurementOrderId} not found");

        if (order.Status != ProcurementStatus.Paid)
            throw new BusinessException("Order must be paid before receiving");

        // External source stores already had stock deducted at order creation
        bool sourceIsInternal = order.SourceStore != null && !order.SourceStore.IsExternal;

        var inventoryErrors = new List<string>();

        foreach (var receivedItem in dto.Items)
        {
            var orderItem = order.Items.FirstOrDefault(i => i.Id == receivedItem.ItemId);
            if (orderItem == null)
                throw new NotFoundException($"Order item with ID {receivedItem.ItemId} not found");

            if (receivedItem.ReceivedQuantity > orderItem.Quantity)
                throw new BusinessException(
                    $"Received quantity ({receivedItem.ReceivedQuantity}) cannot exceed ordered quantity ({orderItem.Quantity})"
                );

            // Fix 14: Match po StoreProductId direktno u destination store-u (ne po nazivu)
            // Tražimo isti store product po imenu SAMO ako ne postoji direktna veza
            // Idealno: ProcurementOrderItem treba DestinationStoreProductId - za sada koristimo robustno matchovanje
            var destinationProduct = await _context.StoreProducts
                .FirstOrDefaultAsync(p => p.StoreId == order.StoreId && p.Id == orderItem.StoreProductId);

            // Fallback: ako source i destination store su različiti, matchujemo po imenu
            if (destinationProduct == null)
                destinationProduct = await _context.StoreProducts
                    .FirstOrDefaultAsync(p => p.StoreId == order.StoreId && p.Name == orderItem.StoreProduct.Name);

            if (destinationProduct != null)
            {
                destinationProduct.CurrentStock += receivedItem.ReceivedQuantity;
                destinationProduct.LastRestocked = DateTime.UtcNow;

                _context.InventoryLogs.Add(new InventoryLog
                {
                    Id = Guid.NewGuid(),
                    StoreProductId = destinationProduct.Id,
                    QuantityChange = receivedItem.ReceivedQuantity,
                    Type = InventoryLogType.Restock,
                    Reason = $"Procurement Order {order.Id} - Received {receivedItem.ReceivedQuantity}/{orderItem.Quantity}",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Fix 14: Ako inventory ne može biti ažuriran, bilježimo grešku i NE postavljamo Received
                var errorMsg = $"No matching product '{orderItem.StoreProduct.Name}' found in destination store {order.StoreId}";
                inventoryErrors.Add(errorMsg);
                _logger.LogWarning("Procurement receive error for order {OrderId}: {Error}", procurementOrderId, errorMsg);
            }

            // Decrease source store stock only for internal stores (external already deducted at creation)
            if (order.SourceStoreId.HasValue && sourceIsInternal)
            {
                orderItem.StoreProduct.CurrentStock -= receivedItem.ReceivedQuantity;

                _context.InventoryLogs.Add(new InventoryLog
                {
                    Id = Guid.NewGuid(),
                    StoreProductId = orderItem.StoreProductId,
                    QuantityChange = -receivedItem.ReceivedQuantity,
                    Type = InventoryLogType.Sale,
                    Reason = $"Procurement Order {order.Id} - Transferred {receivedItem.ReceivedQuantity} to store {order.StoreId}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            _logger.LogInformation("Received {ReceivedQty}/{OrderedQty} of {ProductName} for procurement {OrderId}",
                receivedItem.ReceivedQuantity, orderItem.Quantity, orderItem.StoreProduct.Name, procurementOrderId);
        }

        // Fix 14: Ne postavljamo Received ako inventory nije stvarno ažuriran
        if (inventoryErrors.Count > 0)
            throw new BusinessException($"Cannot mark order as Received - inventory update failed for: {string.Join("; ", inventoryErrors)}");

        // Update order status - SAMO ako je inventory stvarno ažuriran
        order.Status = ProcurementStatus.Received;
        order.DeliveryDate = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(dto.Notes))
            order.Notes = $"{order.Notes}\n[RECEIVED: {dto.Notes}]";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Procurement order {OrderId} fully received and inventory updated", procurementOrderId);
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid procurementOrderId)
    {
        var order = await _context.ProcurementOrders.FindAsync(procurementOrderId);
        if (order == null)
            throw new NotFoundException($"Procurement order with ID {procurementOrderId} not found");

        if (order.Status != ProcurementStatus.Pending)
            throw new BusinessException("Order is not in pending status");

        return await _stripeService.CreateCheckoutSessionAsync(procurementOrderId.ToString(), order.TotalAmount, "bam");
    }

    public async Task<string> HandleCheckoutSuccessAsync(Guid procurementOrderId, string sessionId)
    {
        var order = await _context.ProcurementOrders.FindAsync(procurementOrderId);
        if (order == null)
            throw new NotFoundException($"Procurement order with ID {procurementOrderId} not found");

        var session = await _stripeService.GetCheckoutSessionAsync(sessionId);
        if (session.PaymentStatus != "paid")
            throw new BusinessException($"Payment not completed. Status: {session.PaymentStatus}");

        if (order.Status == ProcurementStatus.Pending)
        {
            order.Status = ProcurementStatus.Paid;
            order.StripePaymentIntentId = session.PaymentIntentId;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Payment successful for procurement order {OrderId}", procurementOrderId);
        }

        return session.PaymentIntentId ?? string.Empty;
    }

    public async Task HandleWebhookCheckoutCompletedAsync(WebhookEventDto eventDto)
    {
        if (!string.IsNullOrEmpty(eventDto.ProcurementOrderId) &&
            Guid.TryParse(eventDto.ProcurementOrderId, out var metadataOrderId))
        {
            var order = await _context.ProcurementOrders.FindAsync(metadataOrderId);
            if (order != null)
            {
                if (order.Status == ProcurementStatus.Paid) return;
                if (order.Status != ProcurementStatus.Pending) return;
                order.Status = ProcurementStatus.Paid;
                order.StripePaymentIntentId = eventDto.PaymentIntentId;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Procurement order {OrderId} marked as PAID via checkout metadata", order.Id);
                return;
            }
        }

        if (!string.IsNullOrEmpty(eventDto.PaymentIntentId))
        {
            var order = await _context.ProcurementOrders
                .FirstOrDefaultAsync(o => o.StripePaymentIntentId == eventDto.PaymentIntentId);
            if (order != null)
            {
                if (order.Status == ProcurementStatus.Paid) return;
                if (order.Status != ProcurementStatus.Pending) return;
                order.Status = ProcurementStatus.Paid;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Procurement order {OrderId} marked as PAID via PI fallback", order.Id);
            }
        }
    }

    public async Task HandleWebhookPaymentSucceededAsync(WebhookEventDto eventDto)
    {
        var order = await _context.ProcurementOrders
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == eventDto.PaymentIntentId);

        if (order == null &&
            !string.IsNullOrEmpty(eventDto.ProcurementOrderId) &&
            Guid.TryParse(eventDto.ProcurementOrderId, out var procId))
        {
            order = await _context.ProcurementOrders.FindAsync(procId);
        }

        if (order == null) return;
        if (order.Status != ProcurementStatus.Pending) return;

        order.Status = ProcurementStatus.Paid;
        if (string.IsNullOrEmpty(order.StripePaymentIntentId))
            order.StripePaymentIntentId = eventDto.PaymentIntentId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Procurement order {OrderId} marked as PAID via payment_intent.succeeded", order.Id);
    }

    public async Task HandleWebhookChargeRefundedAsync(WebhookEventDto eventDto)
    {
        var order = await _context.ProcurementOrders
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == eventDto.PaymentIntentId);

        if (order != null)
            _logger.LogInformation("Procurement order {OrderId} refunded", order.Id);

        await Task.CompletedTask;
    }
}
