using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@test.com", Password = "password123" };
        var expectedResponse = new AuthResponseDto
        {
            UserId = Guid.NewGuid(),
            Email = "test@test.com",
            FullName = "Test User",
            Role = "Admin",
            AccessToken = "fake-jwt-token"
        };

        _authServiceMock.Setup(x => x.LoginAsync(loginDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
        response.Email.Should().Be("test@test.com");
        response.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@test.com", Password = "wrong" };
        _authServiceMock.Setup(x => x.LoginAsync(loginDto))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password"));

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Register_ValidData_ReturnsOkWithToken()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "New User",
            Email = "new@test.com",
            Password = "password123",
            Role = "Waiter"
        };

        var expectedResponse = new AuthResponseDto
        {
            UserId = Guid.NewGuid(),
            Email = "new@test.com",
            FullName = "New User",
            Role = "Waiter",
            AccessToken = "fake-jwt-token"
        };

        _authServiceMock.Setup(x => x.RegisterAsync(registerDto))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
        response.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task Register_EmailAlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test",
            Email = "existing@test.com",
            Password = "password123",
            Role = "Waiter"
        };

        _authServiceMock.Setup(x => x.RegisterAsync(registerDto))
            .ThrowsAsync(new InvalidOperationException("Email already exists"));

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var token = "valid-token";
        _authServiceMock.Setup(x => x.ValidateTokenAsync(token))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateToken(token);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<object>().Subject;
    }
}
