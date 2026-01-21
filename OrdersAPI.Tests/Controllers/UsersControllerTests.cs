using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Enums;
using System.Security.Claims;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UsersController _controller;
    private readonly Guid _testUserId;

    public UsersControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _controller = new UsersController(_userServiceMock.Object);
        _testUserId = Guid.NewGuid();

        SetupAuthenticatedUser();
    }

    #region GetUsers Tests

    [Fact]
    public async Task GetUsers_ReturnsAllUsers()
    {
        // Arrange
        var expectedUsers = new List<UserDto>
        {
            new UserDto
            {
                Id = Guid.NewGuid(),
                FullName = "John Doe",
                Email = "john@example.com",
                Role = "Waiter",
                PhoneNumber = "+387 61 123 456",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new UserDto
            {
                Id = Guid.NewGuid(),
                FullName = "Jane Smith",
                Email = "jane@example.com",
                Role = "Bartender",
                PhoneNumber = "+387 61 234 567",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new UserDto
            {
                Id = Guid.NewGuid(),
                FullName = "Bob Wilson",
                Email = "bob@example.com",
                Role = "Admin",
                PhoneNumber = null,
                IsActive = false,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            }
        };

        _userServiceMock
            .Setup(x => x.GetAllUsersAsync())
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetUsers();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var users = okResult!.Value as IEnumerable<UserDto>;

        users.Should().NotBeNull();
        users.Should().HaveCount(3);
        users!.First().FullName.Should().Be("John Doe");
        users.ElementAt(1).Role.Should().Be("Bartender");
        users.Last().IsActive.Should().BeFalse();

        _userServiceMock.Verify(x => x.GetAllUsersAsync(), Times.Once);
    }

    #endregion

    #region GetActiveUsers Tests

    [Fact]
    public async Task GetActiveUsers_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var expectedUsers = new List<UserDto>
        {
            new UserDto
            {
                Id = Guid.NewGuid(),
                FullName = "John Doe",
                Email = "john@example.com",
                Role = "Waiter",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new UserDto
            {
                Id = Guid.NewGuid(),
                FullName = "Jane Smith",
                Email = "jane@example.com",
                Role = "Bartender",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _userServiceMock
            .Setup(x => x.GetActiveUsersAsync())
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetActiveUsers();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var users = okResult!.Value as IEnumerable<UserDto>;

        users.Should().NotBeNull();
        users.Should().HaveCount(2);
        users!.All(u => u.IsActive).Should().BeTrue();

        _userServiceMock.Verify(x => x.GetActiveUsersAsync(), Times.Once);
    }

    #endregion

    #region GetUsersByRole Tests

    [Fact]
    public async Task GetUsersByRole_Waiter_ReturnsWaiters()
    {
        // Arrange
        var expectedUsers = new List<UserDto>
        {
            new UserDto
            {
                Id = Guid.NewGuid(),
                FullName = "John Doe",
                Email = "john@example.com",
                Role = "Waiter",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new UserDto
            {
                Id = Guid.NewGuid(),
                FullName = "Sarah Lee",
                Email = "sarah@example.com",
                Role = "Waiter",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _userServiceMock
            .Setup(x => x.GetUsersByRoleAsync(UserRole.Waiter))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetUsersByRole("Waiter");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var users = okResult!.Value as IEnumerable<UserDto>;

        users.Should().NotBeNull();
        users.Should().HaveCount(2);
        users!.All(u => u.Role == "Waiter").Should().BeTrue();

        _userServiceMock.Verify(x => x.GetUsersByRoleAsync(UserRole.Waiter), Times.Once);
    }

    [Fact]
    public async Task GetUsersByRole_Bartender_ReturnsBartenders()
    {
        // Arrange
        var expectedUsers = new List<UserDto>
        {
            new UserDto
            {
                Id = Guid.NewGuid(),
                FullName = "Tom Bartender",
                Email = "tom@example.com",
                Role = "Bartender",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _userServiceMock
            .Setup(x => x.GetUsersByRoleAsync(UserRole.Bartender))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetUsersByRole("Bartender");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var users = okResult!.Value as IEnumerable<UserDto>;

        users.Should().NotBeNull();
        users.Should().HaveCount(1);
        users!.First().Role.Should().Be("Bartender");

        _userServiceMock.Verify(x => x.GetUsersByRoleAsync(UserRole.Bartender), Times.Once);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Waiter")]
    [InlineData("Bartender")]
    public async Task GetUsersByRole_AllValidRoles_ReturnsUsersByRole(string roleString)
    {
        // Arrange
        var role = Enum.Parse<UserRole>(roleString, true);
        var expectedUsers = new List<UserDto>
        {
            new UserDto
            {
                Id = Guid.NewGuid(),
                FullName = $"Test {roleString}",
                Email = $"{roleString.ToLower()}@example.com",
                Role = roleString,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        _userServiceMock
            .Setup(x => x.GetUsersByRoleAsync(role))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _controller.GetUsersByRole(roleString);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var users = okResult!.Value as IEnumerable<UserDto>;

        users.Should().NotBeNull();
        users!.All(u => u.Role == roleString).Should().BeTrue();

        _userServiceMock.Verify(x => x.GetUsersByRoleAsync(role), Times.Once);
    }

    #endregion

    #region GetUser Tests

    [Fact]
    public async Task GetUser_ExistingId_ReturnsUser()
    {
        // Arrange
        var expectedUser = new UserDto
        {
            Id = _testUserId,
            FullName = "John Doe",
            Email = "john@example.com",
            Role = "Waiter",
            PhoneNumber = "+387 61 123 456",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            UpdatedAt = DateTime.UtcNow
        };

        _userServiceMock
            .Setup(x => x.GetUserByIdAsync(_testUserId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.GetUser(_testUserId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var user = okResult!.Value as UserDto;

        user.Should().NotBeNull();
        user!.Id.Should().Be(_testUserId);
        user.FullName.Should().Be("John Doe");
        user.Email.Should().Be("john@example.com");
        user.Role.Should().Be("Waiter");
        user.IsActive.Should().BeTrue();

        _userServiceMock.Verify(x => x.GetUserByIdAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task GetUser_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.GetUserByIdAsync(_testUserId))
            .ThrowsAsync(new KeyNotFoundException($"User with ID {_testUserId} not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.GetUser(_testUserId));

        _userServiceMock.Verify(x => x.GetUserByIdAsync(_testUserId), Times.Once);
    }

    #endregion

    #region CreateUser Tests

    [Fact]
    public async Task CreateUser_ValidData_ReturnsCreatedUser()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            FullName = "New Waiter",
            Email = "newwaiter@example.com",
            Password = "SecurePass123",
            Role = "Waiter",
            PhoneNumber = "+387 61 999 888"
        };

        var expectedUser = new UserDto
        {
            Id = _testUserId,
            FullName = "New Waiter",
            Email = "newwaiter@example.com",
            Role = "Waiter",
            PhoneNumber = "+387 61 999 888",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _userServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.CreateUser(createDto);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var user = createdResult!.Value as UserDto;

        user.Should().NotBeNull();
        user!.Id.Should().Be(_testUserId);
        user.FullName.Should().Be("New Waiter");
        user.Email.Should().Be("newwaiter@example.com");
        user.Role.Should().Be("Waiter");
        user.IsActive.Should().BeTrue();

        createdResult.ActionName.Should().Be(nameof(_controller.GetUser));
        createdResult.RouteValues!["id"].Should().Be(_testUserId);

        _userServiceMock.Verify(x => x.CreateUserAsync(It.IsAny<CreateUserDto>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var createDto = new CreateUserDto
        {
            FullName = "Duplicate User",
            Email = "existing@example.com",
            Password = "SecurePass123",
            Role = "Waiter"
        };

        _userServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>()))
            .ThrowsAsync(new InvalidOperationException("User with this email already exists"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.CreateUser(createDto));

        _userServiceMock.Verify(x => x.CreateUserAsync(It.IsAny<CreateUserDto>()), Times.Once);
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public async Task UpdateUser_ValidData_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            FullName = "Updated Name",
            PhoneNumber = "+387 61 111 222",
            IsActive = true
        };

        _userServiceMock
            .Setup(x => x.UpdateUserAsync(_testUserId, It.IsAny<UpdateUserDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateUser(_testUserId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _userServiceMock.Verify(x => x.UpdateUserAsync(_testUserId, It.IsAny<UpdateUserDto>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_PartialUpdate_ReturnsNoContent()
    {
        // Arrange
        var updateDto = new UpdateUserDto
        {
            PhoneNumber = "+387 61 333 444"
        };

        _userServiceMock
            .Setup(x => x.UpdateUserAsync(_testUserId, It.IsAny<UpdateUserDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateUser(_testUserId, updateDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _userServiceMock.Verify(x => x.UpdateUserAsync(_testUserId, It.IsAny<UpdateUserDto>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_NonExistingId_ThrowsKeyNotFoundException()
    {
        // Arrange
        var updateDto = new UpdateUserDto { FullName = "Test" };

        _userServiceMock
            .Setup(x => x.UpdateUserAsync(_testUserId, It.IsAny<UpdateUserDto>()))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.UpdateUser(_testUserId, updateDto));

        _userServiceMock.Verify(x => x.UpdateUserAsync(_testUserId, It.IsAny<UpdateUserDto>()), Times.Once);
    }

    #endregion

    #region DeactivateUser Tests

    [Fact]
    public async Task DeactivateUser_ActiveUser_ReturnsNoContent()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeactivateUserAsync(_testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeactivateUser(_testUserId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _userServiceMock.Verify(x => x.DeactivateUserAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task DeactivateUser_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeactivateUserAsync(_testUserId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.DeactivateUser(_testUserId));

        _userServiceMock.Verify(x => x.DeactivateUserAsync(_testUserId), Times.Once);
    }

    #endregion

    #region ActivateUser Tests

    [Fact]
    public async Task ActivateUser_InactiveUser_ReturnsNoContent()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.ActivateUserAsync(_testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ActivateUser(_testUserId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _userServiceMock.Verify(x => x.ActivateUserAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task ActivateUser_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.ActivateUserAsync(_testUserId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.ActivateUser(_testUserId));

        _userServiceMock.Verify(x => x.ActivateUserAsync(_testUserId), Times.Once);
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public async Task DeleteUser_ExistingUser_ReturnsNoContent()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeleteUserAsync(_testUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteUser(_testUserId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _userServiceMock.Verify(x => x.DeleteUserAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_NonExistingUser_ThrowsKeyNotFoundException()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeleteUserAsync(_testUserId))
            .ThrowsAsync(new KeyNotFoundException("User not found"));

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _controller.DeleteUser(_testUserId));

        _userServiceMock.Verify(x => x.DeleteUserAsync(_testUserId), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_LastAdmin_ThrowsInvalidOperationException()
    {
        // Arrange
        _userServiceMock
            .Setup(x => x.DeleteUserAsync(_testUserId))
            .ThrowsAsync(new InvalidOperationException("Cannot delete the last admin user"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _controller.DeleteUser(_testUserId));

        _userServiceMock.Verify(x => x.DeleteUserAsync(_testUserId), Times.Once);
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
