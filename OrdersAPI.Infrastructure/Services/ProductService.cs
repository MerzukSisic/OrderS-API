using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ApplicationDbContext context, IMapper mapper, ILogger<ProductService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(Guid? categoryId = null, bool? isAvailable = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (isAvailable.HasValue)
            query = query.Where(p => p.IsAvailable == isAvailable.Value);

        var products = await query.ToListAsync();
        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }

    public async Task<ProductDto> GetProductByIdAsync(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            throw new KeyNotFoundException($"Product {id} not found");

        return _mapper.Map<ProductDto>(product);
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            CategoryId = dto.CategoryId,
            ImageUrl = dto.ImageUrl,
            Location = Enum.Parse<PreparationLocation>(dto.PreparationLocation),
            PreparationTimeMinutes = dto.PreparationTimeMinutes,
            Stock = dto.Stock,
            IsAvailable = true
        };

        _context.Products.Add(product);

        // Dodaj sastojke
        foreach (var ingredientDto in dto.Ingredients)
        {
            var ingredient = new ProductIngredient
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                StoreProductId = ingredientDto.StoreProductId,
                Quantity = ingredientDto.Quantity
            };
            _context.ProductIngredients.Add(ingredient);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Product {ProductId} created: {ProductName}", product.Id, product.Name);

        return await GetProductByIdAsync(product.Id);
    }

    public async Task UpdateProductAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product {id} not found");

        if (dto.Name != null) product.Name = dto.Name;
        if (dto.Description != null) product.Description = dto.Description;
        if (dto.Price.HasValue) product.Price = dto.Price.Value;
        if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId.Value;
        if (dto.ImageUrl != null) product.ImageUrl = dto.ImageUrl;
        if (dto.IsAvailable.HasValue) product.IsAvailable = dto.IsAvailable.Value;
        if (dto.PreparationLocation != null) 
            product.Location = Enum.Parse<PreparationLocation>(dto.PreparationLocation);
        if (dto.PreparationTimeMinutes.HasValue) 
            product.PreparationTimeMinutes = dto.PreparationTimeMinutes.Value;
        if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;

        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Product {ProductId} updated", id);
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            throw new KeyNotFoundException($"Product {id} not found");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Product {ProductId} deleted", id);
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductIngredients)
                .ThenInclude(pi => pi.StoreProduct)
            .Where(p => p.Name.Contains(searchTerm) || 
                       (p.Description != null && p.Description.Contains(searchTerm)))
            .ToListAsync();

        return _mapper.Map<IEnumerable<ProductDto>>(products);
    }
}

