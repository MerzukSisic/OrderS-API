using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Tests.Helpers;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class ProductsControllerTests : TestBase
{
    private readonly Mock<IProductService> _productServiceMock;
    private readonly Mock<ILogger<ProductsController>> _loggerMock;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _productServiceMock = new Mock<IProductService>();
        _loggerMock = new Mock<ILogger<ProductsController>>();
        _controller = new ProductsController(_productServiceMock.Object, _loggerMock.Object);
        _controller.ControllerContext = CreateControllerContext(Guid.NewGuid());
    }

    [Fact]
    public async Task GetProducts_ReturnsOkWithProducts()
    {
        // Arrange
        var products = new List<ProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 10.50m },
            new() { Id = Guid.NewGuid(), Name = "Product 2", Price = 15.00m }
        };

        _productServiceMock.Setup(x => x.GetAllProductsAsync(null, null))
            .ReturnsAsync(products);

        // Act
        var result = await _controller.GetProducts();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducts = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>().Subject;
        returnedProducts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProduct_ExistingId_ReturnsOkWithProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new ProductDto { Id = productId, Name = "Test Product", Price = 10.50m };

        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _controller.GetProduct(productId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProduct = okResult.Value.Should().BeOfType<ProductDto>().Subject;
        returnedProduct.Id.Should().Be(productId);
        returnedProduct.Name.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetProduct_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _productServiceMock.Setup(x => x.GetProductByIdAsync(productId))
            .ThrowsAsync(new KeyNotFoundException("Product {productId} not found"));

        // Act
        var result = await _controller.GetProduct(productId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateProduct_ValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "New Product",
            Price = 20.00m,
            CategoryId = Guid.NewGuid(),
            PreparationLocation = "Kitchen"
        };

        var createdProduct = new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Price = createDto.Price
        };

        _productServiceMock.Setup(x => x.CreateProductAsync(createDto))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await _controller.CreateProduct(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedProduct = createdResult.Value.Should().BeOfType<ProductDto>().Subject;
        returnedProduct.Name.Should().Be("New Product");
    }

    [Fact]
    public async Task UpdateProduct_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var updateDto = new UpdateProductDto { Name = "Updated Product" };

        _productServiceMock.Setup(x => x.UpdateProductAsync(productId, updateDto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateProduct(productId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteProduct_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _productServiceMock.Setup(x => x.DeleteProductAsync(productId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteProduct(productId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task SearchProducts_ReturnsMatchingProducts()
    {
        // Arrange
        var searchTerm = "Coffee";
        var products = new List<ProductDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Espresso Coffee", Price = 2.50m },
            new() { Id = Guid.NewGuid(), Name = "Latte Coffee", Price = 3.50m }
        };

        _productServiceMock.Setup(x => x.SearchProductsAsync(searchTerm))
            .ReturnsAsync(products);

        // Act
        var result = await _controller.SearchProducts(searchTerm);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedProducts = okResult.Value.Should().BeAssignableTo<IEnumerable<ProductDto>>().Subject;
        returnedProducts.Should().HaveCount(2);
    }
}
