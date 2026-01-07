using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class TableService(ApplicationDbContext context, IMapper mapper, ILogger<TableService> logger)
    : ITableService
{
    public async Task<IEnumerable<TableDto>> GetAllTablesAsync()
    {
        var tables = await context.CafeTables
            .Include(t => t.Orders.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled))
            .ToListAsync();

        return mapper.Map<IEnumerable<TableDto>>(tables);
    }

    public async Task<TableDto> GetTableByIdAsync(Guid id)
    {
        var table = await context.CafeTables
            .Include(t => t.Orders.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled))
            .FirstOrDefaultAsync(t => t.Id == id);

        if (table == null)
            throw new KeyNotFoundException($"Table {id} not found");

        return mapper.Map<TableDto>(table);
    }

    public async Task<TableDto> CreateTableAsync(CreateTableDto dto)
    {
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

        logger.LogInformation("Table {TableNumber} created", table.TableNumber);

        return mapper.Map<TableDto>(table);
    }

    public async Task UpdateTableAsync(Guid id, UpdateTableDto dto)
    {
        var table = await context.CafeTables.FindAsync(id);
        if (table == null)
            throw new KeyNotFoundException($"Table {id} not found");

        if (dto.TableNumber != null) table.TableNumber = dto.TableNumber;
        if (dto.Capacity.HasValue) table.Capacity = dto.Capacity.Value;
        if (dto.Status != null) table.Status = Enum.Parse<TableStatus>(dto.Status);
        if (dto.Location != null) table.Location = dto.Location;

        await context.SaveChangesAsync();
        logger.LogInformation("Table {TableId} updated", id);
    }

    public async Task DeleteTableAsync(Guid id)
    {
        var table = await context.CafeTables.FindAsync(id);
        if (table == null)
            throw new KeyNotFoundException($"Table {id} not found");

        context.CafeTables.Remove(table);
        await context.SaveChangesAsync();

        logger.LogInformation("Table {TableId} deleted", id);
    }

    public async Task UpdateTableStatusAsync(Guid id, TableStatus status)
    {
        var table = await context.CafeTables.FindAsync(id);
        if (table == null)
            throw new KeyNotFoundException($"Table {id} not found");

        table.Status = status;
        await context.SaveChangesAsync();

        logger.LogInformation("Table {TableId} status updated to {Status}", id, status);
    }
}

