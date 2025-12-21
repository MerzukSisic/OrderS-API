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

public class CategoriesControllerTests : TestBase
{
    private readonly Mock<ICategoryService> _categoryServiceMock;
    private readonly Mock<ILogger<CategoriesController>> _loggerMock;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<ICategoryService>();
        _loggerMock = new Mock<ILogger<CategoriesController>>();
        _controller = new CategoriesController(_categoryServiceMock.Object, _loggerMock.Object);
        _controller.ControllerContext = CreateControllerContext(Guid.NewGuid());
    }

    [Fact]
    public async Task GetCategories_ReturnsOkWithCategories()
    {
        // Arrange
        var categories = new List<CategoryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Drinks", ProductCount = 10 },
            new() { Id = Guid.NewGuid(), Name = "Food", ProductCount = 15 }
        };

        _categoryServiceMock.Setup(x => x.GetAllCategoriesAsync())
            .ReturnsAsync(categories);

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategories = okResult.Value.Should().BeAssignableTo<IEnumerable<CategoryDto>>().Subject;
        returnedCategories.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetCategory_ExistingId_ReturnsOkWithCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = new CategoryDto { Id = categoryId, Name = "Drinks" };

        _categoryServiceMock.Setup(x => x.GetCategoryByIdAsync(categoryId))
            .ReturnsAsync(category);

        // Act
        var result = await _controller.GetCategory(categoryId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedCategory = okResult.Value.Should().BeOfType<CategoryDto>().Subject;
        returnedCategory.Name.Should().Be("Drinks");
    }

    [Fact]
    public async Task CreateCategory_ValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateCategoryDto { Name = "Desserts" };
        var createdCategory = new CategoryDto { Id = Guid.NewGuid(), Name = "Desserts" };

        _categoryServiceMock.Setup(x => x.CreateCategoryAsync(createDto))
            .ReturnsAsync(createdCategory);

        // Act
        var result = await _controller.CreateCategory(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedCategory = createdResult.Value.Should().BeOfType<CategoryDto>().Subject;
        returnedCategory.Name.Should().Be("Desserts");
    }

    [Fact]
    public async Task UpdateCategory_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var updateDto = new UpdateCategoryDto { Name = "Updated Category" };

        _categoryServiceMock.Setup(x => x.UpdateCategoryAsync(categoryId, updateDto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateCategory(categoryId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteCategory_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        _categoryServiceMock.Setup(x => x.DeleteCategoryAsync(categoryId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteCategory(categoryId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
