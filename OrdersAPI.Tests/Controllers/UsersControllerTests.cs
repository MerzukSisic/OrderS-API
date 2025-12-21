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

public class UsersControllerTests : TestBase
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<UsersController>> _loggerMock;
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<UsersController>>();
        _controller = new UsersController(_userServiceMock.Object, _loggerMock.Object);
        _controller.ControllerContext = CreateControllerContext(Guid.NewGuid());
    }

    [Fact]
    public async Task GetUsers_ReturnsOkWithUsers()
    {
        // Arrange
        var users = new List<UserDto>
        {
            new() { Id = Guid.NewGuid(), FullName = "John Doe", Role = "Admin" },
            new() { Id = Guid.NewGuid(), FullName = "Jane Smith", Role = "Waiter" }
        };

        _userServiceMock.Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetUsers();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUsers = okResult.Value.Should().BeAssignableTo<IEnumerable<UserDto>>().Subject;
        returnedUsers.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUser_ExistingId_ReturnsOkWithUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserDto { Id = userId, FullName = "John Doe", Email = "john@test.com" };

        _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _controller.GetUser(userId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Email.Should().Be("john@test.com");
    }

    [Fact]
    public async Task CreateUser_ValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            FullName = "New User",
            Email = "newuser@test.com",
            Password = "password123",
            Role = "Waiter"
        };

        var createdUser = new UserDto
        {
            Id = Guid.NewGuid(),
            FullName = "New User",
            Email = "newuser@test.com",
            Role = "Waiter"
        };

        _userServiceMock.Setup(x => x.CreateUserAsync(createDto))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedUser = createdResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Email.Should().Be("newuser@test.com");
    }

    [Fact]
    public async Task CreateUser_EmailExists_ReturnsBadRequest()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            FullName = "Test",
            Email = "existing@test.com",
            Password = "password123",
            Role = "Waiter"
        };

        _userServiceMock.Setup(x => x.CreateUserAsync(createDto))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteUser_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(x => x.DeleteUserAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteUser(userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }
}
