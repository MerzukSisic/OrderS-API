using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController(IRecommendationService recommendationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetRecommendations([FromQuery] int count = 5)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var recommendations = await recommendationService.GetRecommendedProductsAsync(userId, count);
        return Ok(recommendations);
    }

    [HttpGet("popular")]
    [AllowAnonymous] // Public endpoint
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetPopular([FromQuery] int count = 10)
    {
        var products = await recommendationService.GetPopularProductsAsync(count);
        return Ok(products);
    }

    [HttpGet("time-based")]
    [AllowAnonymous] // Public endpoint
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetTimeBasedRecommendations([FromQuery] int count = 5)
    {
        var hour = DateTime.UtcNow.Hour;
        var products = await recommendationService.GetTimeBasedRecommendationsAsync(hour, count);
        return Ok(products);
    }
}