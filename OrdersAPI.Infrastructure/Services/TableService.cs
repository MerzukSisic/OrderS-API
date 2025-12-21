using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class TableService : ITableService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<TableService> _logger;

    public TableService(ApplicationDbContext context, IMapper mapper, ILogger<TableService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<TableDto>> GetAllTablesAsync()
    {
        var tables = await _context.CafeTables
            .Include(t => t.Orders.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled))
            .ToListAsync();

        return _mapper.Map<IEnumerable<TableDto>>(tables);
    }

    public async Task<TableDto> GetTableByIdAsync(Guid id)
    {
        var table = await _context.CafeTables
            .Include(t => t.Orders.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled))
            .FirstOrDefaultAsync(t => t.Id == id);

        if (table == null)
            throw new KeyNotFoundException($"Table {id} not found");

        return _mapper.Map<TableDto>(table);
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

        _context.CafeTables.Add(table);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Table {TableNumber} created", table.TableNumber);

        return _mapper.Map<TableDto>(table);
    }

    public async Task UpdateTableAsync(Guid id, UpdateTableDto dto)
    {
        var table = await _context.CafeTables.FindAsync(id);
        if (table == null)
            throw new KeyNotFoundException($"Table {id} not found");

        if (dto.TableNumber != null) table.TableNumber = dto.TableNumber;
        if (dto.Capacity.HasValue) table.Capacity = dto.Capacity.Value;
        if (dto.Status != null) table.Status = Enum.Parse<TableStatus>(dto.Status);
        if (dto.Location != null) table.Location = dto.Location;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Table {TableId} updated", id);
    }

    public async Task DeleteTableAsync(Guid id)
    {
        var table = await _context.CafeTables.FindAsync(id);
        if (table == null)
            throw new KeyNotFoundException($"Table {id} not found");

        _context.CafeTables.Remove(table);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Table {TableId} deleted", id);
    }

    public async Task UpdateTableStatusAsync(Guid id, TableStatus status)
    {
        var table = await _context.CafeTables.FindAsync(id);
        if (table == null)
            throw new KeyNotFoundException($"Table {id} not found");

        table.Status = status;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Table {TableId} status updated to {Status}", id, status);
    }
}

