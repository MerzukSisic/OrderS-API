using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Enums;
using System.Security.Claims;
using OrdersAPI.Domain.Entities;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _productServiceMock;
    private readonly ProductsController _controller;
    private readonly Guid _testProductId;
    private readonly Guid _testCategoryId;

    public ProductsControllerTests()
    {
        _productServiceMock = new Mock<IProductService>();
        _controller = new ProductsController(_productServiceMock.Object);
        _testProductId = Guid.NewGuid();
        _testCategoryId = Guid.NewGuid();

        SetupAuthenticatedUser();
    }

    #region GetProducts Tests

    [Fact]
    public async Task GetProducts_NoFilters_ReturnsAllProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductDto>
        {
            new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = "Pizza Margherita",
                Price = 12.50m,
                CategoryId = _testCategoryId,
                CategoryName = "Pizza",
                IsAvailable = true,
                PreparationLocation = "Kitchen",
                Stock = 100
            },
            new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = "Coca Cola",
                Price = 2.50m,
                CategoryId = Guid.NewGuid(),
                CategoryName = "Drinks",
                IsAvailable = true,
                PreparationLocation = "Bar",
                Stock = 50
            }
        };

        _productServiceMock
            .Setup(x => x.GetAllProductsAsync(null, null))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetProducts(null, null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<ProductDto>;

        products.Should().NotBeNull();
        products.Should().HaveCount(2);
        products!.First().Name.Should().Be("Pizza Margherita");

        _productServiceMock.Verify(x => x.GetAllProductsAsync(null, null), Times.Once);
    }

    [Fact]
    public async Task GetProducts_FilterByCategory_ReturnsFilteredProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductDto>
        {
            new ProductDto
            {
                Id = _testProductId,
                Name = "Pizza Margherita",
                Price = 12.50m,
                CategoryId = _testCategoryId,
                CategoryName = "Pizza",
                IsAvailable = true
            }
        };

        _productServiceMock
            .Setup(x => x.GetAllProductsAsync(_testCategoryId, null))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetProducts(_testCategoryId, null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<ProductDto>;

        products.Should().NotBeNull();
        products.Should().HaveCount(1);
        products!.First().CategoryId.Should().Be(_testCategoryId);

        _productServiceMock.Verify(x => x.GetAllProductsAsync(_testCategoryId, null), Times.Once);
    }

    [Fact]
    public async Task GetProducts_FilterByAvailability_ReturnsOnlyAvailableProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductDto>
        {
            new ProductDto
            {
                Id = _testProductId,
                Name = "Pizza Margherita",
                IsAvailable = true
            }
        };

        _productServiceMock
            .Setup(x => x.GetAllProductsAsync(null, true))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetProducts(null, true);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<ProductDto>;

        products.Should().NotBeNull();
        products!.All(p => p.IsAvailable).Should().BeTrue();

        _productServiceMock.Verify(x => x.GetAllProductsAsync(null, true), Times.Once);
    }

    #endregion

    #region GetProduct Tests

    [Fact]
    public async Task GetProduct_ExistingId_ReturnsProduct()
    {
        // Arrange
        var expectedProduct = new ProductDto
        {
            Id = _testProductId,
            Name = "Pizza Margherita",
            Description = "Classic Italian pizza",
            Price = 12.50m,
            CategoryId = _testCategoryId,
            CategoryName = "Pizza",
            IsAvailable = true,
            PreparationLocation = "Kitchen",
            PreparationTimeMinutes = 15,
            Stock = 100,
            Ingredients = new List<ProductIngredientDto>(),
            AccompanimentGroups = new List<AccompanimentGroupDto>()
        };

        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(_testProductId))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _controller.GetProduct(_testProductId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var product = okResult!.Value as ProductDto;

        product.Should().NotBeNull();
        product!.Id.Should().Be(_testProductId);
        product.Name.Should().Be("Pizza Margherita");
        product.Price.Should().Be(12.50m);

        _productServiceMock.Verify(x => x.GetProductByIdAsync(_testProductId), Times.Once);
    }

    [Fact]
    public async Task GetProduct_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _productServiceMock
            .Setup(x => x.GetProductByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Product with ID {_testProductId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.GetProduct(_testProductId));

        _productServiceMock.Verify(x => x.GetProductByIdAsync(_testProductId), Times.Once);
    }

    #endregion

    #region SearchProducts Tests

    [Fact]
    public async Task SearchProducts_ValidTerm_ReturnsMatchingProducts()
    {
        // Arrange
        var searchTerm = "pizza";
        var expectedProducts = new List<ProductDto>
        {
            new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = "Pizza Margherita",
                Price = 12.50m
            },
            new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = "Pizza Pepperoni",
                Price = 14.00m
            }
        };

        _productServiceMock
            .Setup(x => x.SearchProductsAsync(searchTerm))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.SearchProducts(searchTerm);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<ProductDto>;

        products.Should().NotBeNull();
        products.Should().HaveCount(2);

        _productServiceMock.Verify(x => x.SearchProductsAsync(searchTerm), Times.Once);
    }

    [Fact]
    public async Task SearchProducts_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var searchTerm = "nonexistent";

        _productServiceMock
            .Setup(x => x.SearchProductsAsync(searchTerm))
            .ReturnsAsync(new List<ProductDto>());

        // Act
        var result = await _controller.SearchProducts(searchTerm);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as IEnumerable<ProductDto>;

        products.Should().NotBeNull();
        products.Should().BeEmpty();

        _productServiceMock.Verify(x => x.SearchProductsAsync(searchTerm), Times.Once);
    }

    #endregion

    #region GetProductsByLocation Tests

    [Fact]
    public async Task GetProductsByLocation_Kitchen_ReturnsKitchenProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductDto>
        {
            new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = "Pizza Margherita",
                PreparationLocation = "Kitchen",
                IsAvailable = true
            }
        };

        _productServiceMock
            .Setup(x => x.GetProductsByLocationAsync(PreparationLocation.Kitchen, null))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetProductsByLocation("Kitchen", null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as List<ProductDto>;

        products.Should().NotBeNull();
        products.Should().HaveCount(1);
        products![0].PreparationLocation.Should().Be("Kitchen");

        _productServiceMock.Verify(x => x.GetProductsByLocationAsync(PreparationLocation.Kitchen, null), Times.Once);
    }

    [Fact]
    public async Task GetProductsByLocation_Bar_ReturnsBarProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductDto>
        {
            new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = "Mojito",
                PreparationLocation = "Bar",
                IsAvailable = true
            }
        };

        _productServiceMock
            .Setup(x => x.GetProductsByLocationAsync(PreparationLocation.Bar, null))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetProductsByLocation("Bar", null);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as List<ProductDto>;

        products.Should().NotBeNull();
        products![0].PreparationLocation.Should().Be("Bar");

        _productServiceMock.Verify(x => x.GetProductsByLocationAsync(PreparationLocation.Bar, null), Times.Once);
    }

    [Fact]
    public async Task GetProductsByLocation_WithAvailabilityFilter_ReturnsFilteredProducts()
    {
        // Arrange
        var expectedProducts = new List<ProductDto>
        {
            new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = "Pizza Margherita",
                PreparationLocation = "Kitchen",
                IsAvailable = true
            }
        };

        _productServiceMock
            .Setup(x => x.GetProductsByLocationAsync(PreparationLocation.Kitchen, true))
            .ReturnsAsync(expectedProducts);

        // Act
        var result = await _controller.GetProductsByLocation("Kitchen", true);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var products = okResult!.Value as List<ProductDto>;

        products.Should().NotBeNull();
        products!.All(p => p.IsAvailable).Should().BeTrue();

        _productServiceMock.Verify(x => x.GetProductsByLocationAsync(PreparationLocation.Kitchen, true), Times.Once);
    }

    #endregion

    #region CreateProduct Tests

    [Fact]
    public async Task CreateProduct_ValidData_ReturnsCreatedProduct()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "Pizza Margherita",
            Description = "Classic Italian pizza",
            Price = 12.50m,
            CategoryId = _testCategoryId,
            ImageUrl = "https://example.com/pizza.jpg",
            PreparationLocation = "Kitchen",
            PreparationTimeMinutes = 15,
            Stock = 100,
            Ingredients = new List<CreateProductIngredientDto>()
        };

        var expectedProduct = new ProductDto
        {
            Id = _testProductId,
            Name = "Pizza Margherita",
            Description = "Classic Italian pizza",
            Price = 12.50m,
            CategoryId = _testCategoryId,
            CategoryName = "Pizza",
            IsAvailable = true,
            PreparationLocation = "Kitchen",
            PreparationTimeMinutes = 15,
            Stock = 100,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _productServiceMock
            .Setup(x => x.CreateProductAsync(It.IsAny<CreateProductDto>()))
            .ReturnsAsync(expectedProduct);

        // Act
        var result = await _controller.CreateProduct(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var product = createdResult!.Value as ProductDto;

        product.Should().NotBeNull();
        product!.Id.Should().Be(_testProductId);
        product.Name.Should().Be("Pizza Margherita");
        product.Price.Should().Be(12.50m);

        createdResult.ActionName.Should().Be(nameof(_controller.GetProduct));
        createdResult.RouteValues!["id"].Should().Be(_testProductId);

        _productServiceMock.Verify(x => x.CreateProductAsync(It.IsAny<CreateProductDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateProduct_InvalidCategoryId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var createDto = new CreateProductDto
        {
            Name = "Test Product",
            Price = 10.00m,
            CategoryId = Guid.NewGuid()
        };

        _productServiceMock
            .Setup(x => x.CreateProductAsync(It.IsAny<CreateProductDto>()))
            .ThrowsAsync(new KeyNotFoundException("Category not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.CreateProduct(createDto));

        _productServiceMock.Verify(x => x.CreateProductAsync(It.IsAny<CreateProductDto>()), Times.Once);
    }

    #endregion

    #region UpdateProduct Tests

    [Fact]
    public async Task UpdateProduct_ValidData_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateProductDto
        {
            Name = "Updated Pizza",
            Price = 15.00m,
            IsAvailable = false
        };

        _productServiceMock
            .Setup(x => x.UpdateProductAsync(_testProductId, It.IsAny<UpdateProductDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateProduct(_testProductId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _productServiceMock.Verify(x => x.UpdateProductAsync(_testProductId, It.IsAny<UpdateProductDto>()), Times.Once);
    }

    [Fact]
    public async Task UpdateProduct_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateProductDto { Name = "Test" };

        _productServiceMock
            .Setup(x => x.UpdateProductAsync(_testProductId, It.IsAny<UpdateProductDto>()))
            .ThrowsAsync(new KeyNotFoundException("Product not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.UpdateProduct(_testProductId, updateDto));

        _productServiceMock.Verify(x => x.UpdateProductAsync(_testProductId, It.IsAny<UpdateProductDto>()), Times.Once);
    }

    #endregion

    #region ToggleAvailability Tests

    [Fact]
    public async Task ToggleAvailability_MakesProductUnavailable_ReturnsFalse()
    {
        // Arrange
        _productServiceMock
            .Setup(x => x.ToggleAvailabilityAsync(_testProductId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ToggleAvailability(_testProductId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { isAvailable = false });

        _productServiceMock.Verify(x => x.ToggleAvailabilityAsync(_testProductId), Times.Once);
    }

    [Fact]
    public async Task ToggleAvailability_MakesProductAvailable_ReturnsTrue()
    {
        // Arrange
        _productServiceMock
            .Setup(x => x.ToggleAvailabilityAsync(_testProductId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ToggleAvailability(_testProductId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { isAvailable = true });

        _productServiceMock.Verify(x => x.ToggleAvailabilityAsync(_testProductId), Times.Once);
    }

    [Fact]
    public async Task ToggleAvailability_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _productServiceMock
            .Setup(x => x.ToggleAvailabilityAsync(_testProductId))
            .ThrowsAsync(new KeyNotFoundException("Product not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.ToggleAvailability(_testProductId));

        _productServiceMock.Verify(x => x.ToggleAvailabilityAsync(_testProductId), Times.Once);
    }

    #endregion

    #region BulkUpdateAvailability Tests

    [Fact]
    public async Task BulkUpdateAvailability_MultipleProducts_ReturnsNoContent()
    {
        // Arrange
        var productIds = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        var bulkUpdateDto = new BulkUpdateAvailabilityDto
        {
            ProductIds = productIds,
            IsAvailable = false
        };

        _productServiceMock
            .Setup(x => x.BulkUpdateAvailabilityAsync(
                It.Is<List<Guid>>(ids => ids.SequenceEqual(productIds)),
                false))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.BulkUpdateAvailability(bulkUpdateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _productServiceMock.Verify(x => x.BulkUpdateAvailabilityAsync(
            It.Is<List<Guid>>(ids => ids.SequenceEqual(productIds)),
            false), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateAvailability_MakeProductsAvailable_ReturnsNoContent()
    {
        // Arrange
        var productIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        var bulkUpdateDto = new BulkUpdateAvailabilityDto
        {
            ProductIds = productIds,
            IsAvailable = true
        };

        _productServiceMock
            .Setup(x => x.BulkUpdateAvailabilityAsync(It.IsAny<List<Guid>>(), true))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.BulkUpdateAvailability(bulkUpdateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _productServiceMock.Verify(x => x.BulkUpdateAvailabilityAsync(It.IsAny<List<Guid>>(), true), Times.Once);
    }

    [Fact]
    public async Task BulkUpdateAvailability_EmptyList_ReturnsNoContent()
    {
        // Arrange
        var bulkUpdateDto = new BulkUpdateAvailabilityDto
        {
            ProductIds = new List<Guid>(),
            IsAvailable = false
        };

        _productServiceMock
            .Setup(x => x.BulkUpdateAvailabilityAsync(It.IsAny<List<Guid>>(), false))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.BulkUpdateAvailability(bulkUpdateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _productServiceMock.Verify(x => x.BulkUpdateAvailabilityAsync(It.IsAny<List<Guid>>(), false), Times.Once);
    }

    #endregion

    #region DeleteProduct Tests

    [Fact]
    public async Task DeleteProduct_ExistingId_ReturnsNoContent()
    {
        // Arrange
        _productServiceMock
            .Setup(x => x.DeleteProductAsync(_testProductId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteProduct(_testProductId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _productServiceMock.Verify(x => x.DeleteProductAsync(_testProductId), Times.Once);
    }

    [Fact]
    public async Task DeleteProduct_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _productServiceMock
            .Setup(x => x.DeleteProductAsync(_testProductId))
            .ThrowsAsync(new KeyNotFoundException("Product not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.DeleteProduct(_testProductId));

        _productServiceMock.Verify(x => x.DeleteProductAsync(_testProductId), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Email, "admin@example.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    #endregion
}
