using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ILogger<RecommendationsController> _logger;

    public RecommendationsController(IRecommendationService recommendationService, ILogger<RecommendationsController> logger)
    {
        _recommendationService = recommendationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetRecommendations([FromQuery] int count = 5)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var recommendations = await _recommendationService.GetRecommendedProductsAsync(userId, count);
        return Ok(recommendations);
    }

    [HttpGet("popular")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetPopular([FromQuery] int count = 10)
    {
        var products = await _recommendationService.GetPopularProductsAsync(count);
        return Ok(products);
    }

    [HttpGet("time-based")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetTimeBasedRecommendations([FromQuery] int count = 5)
    {
        var hour = DateTime.Now.Hour;
        var products = await _recommendationService.GetTimeBasedRecommendationsAsync(hour, count);
        return Ok(products);
    }
}
