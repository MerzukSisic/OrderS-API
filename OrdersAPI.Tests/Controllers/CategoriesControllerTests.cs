using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly CategoriesController _controller;
    private readonly Guid _testCategoryId;

    public CategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _controller = new CategoriesController(_categoryServiceMock.Object);
        _testCategoryId = Guid.NewGuid();
    }

    #region GetCategories Tests

    [Fact]
    public async Task GetCategories_ReturnsAllCategories()
    {
        // Arrange
        var expectedCategories = new List<CategoryDto>
        {
            new CategoryDto
            {
                Id = Guid.NewGuid(),
                Name = "Burgers",
                Description = "Delicious burgers",
                IconName = "burger-icon",
                ProductCount = 5,
                CreatedAt = DateTime.UtcNow
            },
            new CategoryDto
            {
                Id = Guid.NewGuid(),
                Name = "Pizza",
                Description = "Italian pizza",
                IconName = "pizza-icon",
                ProductCount = 8,
                CreatedAt = DateTime.UtcNow
            },
            new CategoryDto
            {
                Id = Guid.NewGuid(),
                Name = "Drinks",
                Description = "Beverages",
                IconName = "drink-icon",
                ProductCount = 12,
                CreatedAt = DateTime.UtcNow
            }
        };

        _categoryServiceMock
            .Setup(x => x.GetAllCategoriesAsync())
            .ReturnsAsync(expectedCategories);

        // Act
        var result = await _controller.GetCategories();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var categories = okResult!.Value as IEnumerable<CategoryDto>;

        categories.Should().NotBeNull();
        categories.Should().HaveCount(3);
        categories!.First().Name.Should().Be("Burgers");
        categories.First().ProductCount.Should().Be(5);
        categories.Last().Name.Should().Be("Drinks");
        categories.Last().IconName.Should().Be("drink-icon");

        _categoryServiceMock.Verify(x => x.GetAllCategoriesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCategories_NoCategories_ReturnsEmptyList()
    {
        // Arrange
        _categoryServiceMock
            .Setup(x => x.GetAllCategoriesAsync())
            .ReturnsAsync(new List<CategoryDto>());

        // Act
        var result = await _controller.GetCategories();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var categories = okResult!.Value as IEnumerable<CategoryDto>;

        categories.Should().NotBeNull();
        categories.Should().BeEmpty();

        _categoryServiceMock.Verify(x => x.GetAllCategoriesAsync(), Times.Once);
    }

    #endregion

    #region GetCategory Tests

    [Fact]
    public async Task GetCategory_ExistingId_ReturnsCategory()
    {
        // Arrange
        var expectedCategory = new CategoryDto
        {
            Id = _testCategoryId,
            Name = "Burgers",
            Description = "Delicious burgers",
            IconName = "burger-icon",
            ProductCount = 5,
            CreatedAt = DateTime.UtcNow
        };

        _categoryServiceMock
            .Setup(x => x.GetCategoryByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedCategory);

        // Act
        var result = await _controller.GetCategory(_testCategoryId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var category = okResult!.Value as CategoryDto;

        category.Should().NotBeNull();
        category!.Id.Should().Be(_testCategoryId);
        category.Name.Should().Be("Burgers");
        category.Description.Should().Be("Delicious burgers");
        category.IconName.Should().Be("burger-icon");
        category.ProductCount.Should().Be(5);

        _categoryServiceMock.Verify(x => x.GetCategoryByIdAsync(_testCategoryId), Times.Once);
    }

    [Fact]
    public async Task GetCategory_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _categoryServiceMock
            .Setup(x => x.GetCategoryByIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Category with ID {_testCategoryId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.GetCategory(_testCategoryId));

        _categoryServiceMock.Verify(x => x.GetCategoryByIdAsync(_testCategoryId), Times.Once);
    }

    #endregion

    #region GetCategoryWithProducts Tests

    [Fact]
    public async Task GetCategoryWithProducts_ExistingId_ReturnsCategoryWithProducts()
    {
        // Arrange
        var expectedCategory = new CategoryWithProductsDto
        {
            Id = _testCategoryId,
            Name = "Burgers",
            Description = "Delicious burgers",
            IconName = "burger-icon",
            CreatedAt = DateTime.UtcNow,
            Products = new List<ProductSummaryDto>
            {
                new ProductSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Cheeseburger",
                    Description = "Classic cheeseburger",
                    Price = 8.99m,
                    IsAvailable = true
                },
                new ProductSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Bacon Burger",
                    Description = "Burger with bacon",
                    Price = 10.99m,
                    IsAvailable = true
                }
            }
        };

        _categoryServiceMock
            .Setup(x => x.GetCategoryWithProductsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedCategory);

        // Act
        var result = await _controller.GetCategoryWithProducts(_testCategoryId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var category = okResult!.Value as CategoryWithProductsDto;

        category.Should().NotBeNull();
        category!.Id.Should().Be(_testCategoryId);
        category.Name.Should().Be("Burgers");
        category.IconName.Should().Be("burger-icon");
        category.Products.Should().HaveCount(2);
        category.Products.First().Name.Should().Be("Cheeseburger");
        category.Products.Last().Price.Should().Be(10.99m);

        _categoryServiceMock.Verify(x => x.GetCategoryWithProductsAsync(_testCategoryId), Times.Once);
    }

    [Fact]
    public async Task GetCategoryWithProducts_CategoryWithNoProducts_ReturnsEmptyProductsList()
    {
        // Arrange
        var expectedCategory = new CategoryWithProductsDto
        {
            Id = _testCategoryId,
            Name = "Empty Category",
            Description = "No products yet",
            IconName = null,
            CreatedAt = DateTime.UtcNow,
            Products = new List<ProductSummaryDto>()
        };

        _categoryServiceMock
            .Setup(x => x.GetCategoryWithProductsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedCategory);

        // Act
        var result = await _controller.GetCategoryWithProducts(_testCategoryId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var category = okResult!.Value as CategoryWithProductsDto;

        category.Should().NotBeNull();
        category!.Products.Should().BeEmpty();

        _categoryServiceMock.Verify(x => x.GetCategoryWithProductsAsync(_testCategoryId), Times.Once);
    }

    [Fact]
    public async Task GetCategoryWithProducts_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _categoryServiceMock
            .Setup(x => x.GetCategoryWithProductsAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Category with ID {_testCategoryId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.GetCategoryWithProducts(_testCategoryId));

        _categoryServiceMock.Verify(x => x.GetCategoryWithProductsAsync(_testCategoryId), Times.Once);
    }

    #endregion

    #region CreateCategory Tests

    [Fact]
    public async Task CreateCategory_ValidData_ReturnsCreatedCategory()
    {
        // Arrange
        var createDto = new CreateCategoryDto
        {
            Name = "New Category",
            Description = "Brand new category",
            IconName = "new-icon"
        };

        var expectedCategory = new CategoryDto
        {
            Id = _testCategoryId,
            Name = "New Category",
            Description = "Brand new category",
            IconName = "new-icon",
            ProductCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _categoryServiceMock
            .Setup(x => x.CreateCategoryAsync(It.IsAny<CreateCategoryDto>()))
            .ReturnsAsync(expectedCategory);

        // Act
        var result = await _controller.CreateCategory(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var category = createdResult!.Value as CategoryDto;

        category.Should().NotBeNull();
        category!.Id.Should().Be(_testCategoryId);
        category.Name.Should().Be("New Category");
        category.Description.Should().Be("Brand new category");
        category.IconName.Should().Be("new-icon");
        category.ProductCount.Should().Be(0);

        createdResult.ActionName.Should().Be(nameof(_controller.GetCategory));
        createdResult.RouteValues!["id"].Should().Be(_testCategoryId);

        _categoryServiceMock.Verify(x => x.CreateCategoryAsync(It.IsAny<CreateCategoryDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateCategoryDto
        {
            Name = "Existing Category",
            Description = "This already exists",
            IconName = "icon"
        };

        _categoryServiceMock
            .Setup(x => x.CreateCategoryAsync(It.IsAny<CreateCategoryDto>()))
            .ThrowsAsync(new InvalidOperationException("Category with name 'Existing Category' already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.CreateCategory(createDto));

        _categoryServiceMock.Verify(x => x.CreateCategoryAsync(It.IsAny<CreateCategoryDto>()), Times.Once);
    }

    #endregion

    #region UpdateCategory Tests

    [Fact]
    public async Task UpdateCategory_ValidData_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateCategoryDto
        {
            Name = "Updated Category",
            Description = "Updated description",
            IconName = "updated-icon"
        };

        _categoryServiceMock
            .Setup(x => x.UpdateCategoryAsync(It.IsAny<Guid>(), It.IsAny<UpdateCategoryDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateCategory(_testCategoryId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _categoryServiceMock.Verify(x => x.UpdateCategoryAsync(_testCategoryId, updateDto), Times.Once);
    }

    [Fact]
    public async Task UpdateCategory_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateCategoryDto
        {
            Name = "Updated Category",
            Description = "Updated description",
            IconName = "icon"
        };

        _categoryServiceMock
            .Setup(x => x.UpdateCategoryAsync(It.IsAny<Guid>(), It.IsAny<UpdateCategoryDto>()))
            .ThrowsAsync(new KeyNotFoundException($"Category with ID {_testCategoryId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.UpdateCategory(_testCategoryId, updateDto));

        _categoryServiceMock.Verify(x => x.UpdateCategoryAsync(_testCategoryId, updateDto), Times.Once);
    }

    [Fact]
    public async Task UpdateCategory_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var updateDto = new UpdateCategoryDto
        {
            Name = "Existing Name",
            Description = "Description",
            IconName = "icon"
        };

        _categoryServiceMock
            .Setup(x => x.UpdateCategoryAsync(It.IsAny<Guid>(), It.IsAny<UpdateCategoryDto>()))
            .ThrowsAsync(new InvalidOperationException("Category with name 'Existing Name' already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.UpdateCategory(_testCategoryId, updateDto));

        _categoryServiceMock.Verify(x => x.UpdateCategoryAsync(_testCategoryId, updateDto), Times.Once);
    }

    [Fact]
    public async Task UpdateCategory_PartialUpdate_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateCategoryDto
        {
            Name = "Partial Update",
            Description = null,
            IconName = null
        };

        _categoryServiceMock
            .Setup(x => x.UpdateCategoryAsync(It.IsAny<Guid>(), It.IsAny<UpdateCategoryDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateCategory(_testCategoryId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _categoryServiceMock.Verify(x => x.UpdateCategoryAsync(_testCategoryId, updateDto), Times.Once);
    }

    #endregion

    #region DeleteCategory Tests

    [Fact]
    public async Task DeleteCategory_ExistingId_ReturnsNoContent()
    {
        // Arrange
        _categoryServiceMock
            .Setup(x => x.DeleteCategoryAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteCategory(_testCategoryId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _categoryServiceMock.Verify(x => x.DeleteCategoryAsync(_testCategoryId), Times.Once);
    }

    [Fact]
    public async Task DeleteCategory_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _categoryServiceMock
            .Setup(x => x.DeleteCategoryAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new KeyNotFoundException($"Category with ID {_testCategoryId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _controller.DeleteCategory(_testCategoryId));

        _categoryServiceMock.Verify(x => x.DeleteCategoryAsync(_testCategoryId), Times.Once);
    }

    [Fact]
    public async Task DeleteCategory_CategoryWithProducts_ThrowsInvalidOperationException()
    {
        // Arrange
        _categoryServiceMock
            .Setup(x => x.DeleteCategoryAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete category with existing products"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.DeleteCategory(_testCategoryId));

        _categoryServiceMock.Verify(x => x.DeleteCategoryAsync(_testCategoryId), Times.Once);
    }

    #endregion
}
