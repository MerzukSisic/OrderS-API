using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class TableService(ApplicationDbContext context, ILogger<TableService> logger) : ITableService
{
    public async Task<IEnumerable<TableDto>> GetAllTablesAsync()
    {
        var tables = await context.CafeTables
            .AsNoTracking()
            .Select(t => new TableDto
            {
                Id = t.Id,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                Status = t.Status.ToString(),
                Location = t.Location,
                CurrentOrderId = t.Orders
                    .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => (Guid?)o.Id)
                    .FirstOrDefault(),
                CurrentOrderTotal = t.Orders
                    .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                    .Sum(o => (decimal?)o.TotalAmount),
                ActiveOrderCount = t.Orders
                    .Count(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            })
            .OrderBy(t => t.TableNumber)
            .ToListAsync();

        return tables;
    }

    public async Task<TableDto> GetTableByIdAsync(Guid id)
    {
        var table = await context.CafeTables
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TableDto
            {
                Id = t.Id,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                Status = t.Status.ToString(),
                Location = t.Location,
                CurrentOrderId = t.Orders
                    .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => (Guid?)o.Id)
                    .FirstOrDefault(),
                CurrentOrderTotal = t.Orders
                    .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
                    .Sum(o => (decimal?)o.TotalAmount),
                ActiveOrderCount = t.Orders
                    .Count(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            })
            .FirstOrDefaultAsync();

        if (table == null)
            throw new KeyNotFoundException($"Table with ID {id} not found");

        return table;
    }

    public async Task<TableDto> CreateTableAsync(CreateTableDto dto)
    {
        // Check if table number already exists
        var exists = await context.CafeTables.AnyAsync(t => t.TableNumber == dto.TableNumber);
        if (exists)
            throw new InvalidOperationException($"Table with number {dto.TableNumber} already exists");

        var table = new CafeTable
        {
            Id = Guid.NewGuid(),
            TableNumber = dto.TableNumber,
            Capacity = dto.Capacity,
            Location = dto.Location,
            Status = TableStatus.Available
        };

        context.CafeTables.Add(table);
        await context.SaveChangesAsync();

        logger.LogInformation("Table {TableNumber} created with ID {TableId}", table.TableNumber, table.Id);

        return await GetTableByIdAsync(table.Id);
    }

    public async Task UpdateTableAsync(Guid id, UpdateTableDto dto)
    {
        var table = await context.CafeTables.FindAsync(id);
        if (table == null)
            throw new KeyNotFoundException($"Table with ID {id} not found");

        // Check if new table number conflicts
        if (dto.TableNumber != null && dto.TableNumber != table.TableNumber)
        {
            var exists = await context.CafeTables.AnyAsync(t => t.TableNumber == dto.TableNumber && t.Id != id);
            if (exists)
                throw new InvalidOperationException($"Table with number {dto.TableNumber} already exists");
        }

        if (dto.TableNumber != null) table.TableNumber = dto.TableNumber;
        if (dto.Capacity.HasValue) table.Capacity = dto.Capacity.Value;
        if (dto.Status != null) table.Status = Enum.Parse<TableStatus>(dto.Status);
        if (dto.Location != null) table.Location = dto.Location;

        await context.SaveChangesAsync();

        logger.LogInformation("Table {TableId} updated", id);
    }

    public async Task UpdateTableStatusAsync(Guid id, TableStatus status)
    {
        var table = await context.CafeTables.FindAsync(id);
        if (table == null)
            throw new KeyNotFoundException($"Table with ID {id} not found");

        table.Status = status;
        await context.SaveChangesAsync();

        logger.LogInformation("Table {TableId} ({TableNumber}) status updated to {Status}", 
            id, table.TableNumber, status);
    }

    public async Task DeleteTableAsync(Guid id)
    {
        var table = await context.CafeTables
            .Include(t => t.Orders)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (table == null)
            throw new KeyNotFoundException($"Table with ID {id} not found");

        // Check if table has active orders
        var hasActiveOrders = table.Orders.Any(o => 
            o.Status != OrderStatus.Completed && 
            o.Status != OrderStatus.Cancelled);

        if (hasActiveOrders)
            throw new InvalidOperationException($"Cannot delete table {table.TableNumber} with active orders");

        context.CafeTables.Remove(table);
        await context.SaveChangesAsync();

        logger.LogInformation("Table {TableId} ({TableNumber}) deleted", id, table.TableNumber);
    }
}
