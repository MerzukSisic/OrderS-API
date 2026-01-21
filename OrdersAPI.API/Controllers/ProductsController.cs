using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isAvailable = null)
    {
        var products = await productService.GetAllProductsAsync(categoryId, isAvailable);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var product = await productService.GetProductByIdAsync(id);
        return Ok(product);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts([FromQuery] string term)
    {
        var products = await productService.SearchProductsAsync(term);
        return Ok(products);
    }

    [HttpGet("by-location")]
    public async Task<ActionResult<List<ProductDto>>> GetProductsByLocation(
        [FromQuery] string location,
        [FromQuery] bool? isAvailable = null)
    {
        var preparationLocation = Enum.Parse<PreparationLocation>(location);
        var products = await productService.GetProductsByLocationAsync(preparationLocation, isAvailable);
        return Ok(products);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto dto)
    {
        var product = await productService.CreateProductAsync(dto);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
    {
        await productService.UpdateProductAsync(id, dto);
        return NoContent();
    }

    [HttpPut("{id}/toggle-availability")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> ToggleAvailability(Guid id)
    {
        var isAvailable = await productService.ToggleAvailabilityAsync(id);
        return Ok(new { isAvailable });
    }

    [HttpPut("bulk-availability")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkUpdateAvailability([FromBody] BulkUpdateAvailabilityDto dto)
    {
        await productService.BulkUpdateAvailabilityAsync(dto.ProductIds, dto.IsAvailable);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        await productService.DeleteProductAsync(id);
        return NoContent();
    }
}
