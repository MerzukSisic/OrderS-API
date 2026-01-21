using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OrdersAPI.API.Controllers;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using System.Security.Claims;
using Xunit;

namespace OrdersAPI.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;
    private readonly Guid _testUserId;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
        _testUserId = Guid.NewGuid();
    }

    #region Login Tests

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginDto = new LoginDto 
        { 
            Email = "test@example.com", 
            Password = "Password123" 
        };

        var expectedResponse = new AuthResponseDto
        {
            UserId = _testUserId,
            Email = "test@example.com",
            FullName = "Test User",
            Role = "Admin",
            AccessToken = "fake-jwt-access-token",
            RefreshToken = "fake-refresh-token"
        };

        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as AuthResponseDto;

        response.Should().NotBeNull();
        response!.Email.Should().Be("test@example.com");
        response.FullName.Should().Be("Test User");
        response.Role.Should().Be("Admin");
        response.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();

        _authServiceMock.Verify(x => x.LoginAsync(It.IsAny<LoginDto>()), Times.Once);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ThrowsUnauthorizedException()
    {
        // Arrange
        var loginDto = new LoginDto 
        { 
            Email = "test@example.com", 
            Password = "WrongPassword" 
        };

        _authServiceMock
            .Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.Login(loginDto));
        
        _authServiceMock.Verify(x => x.LoginAsync(It.IsAny<LoginDto>()), Times.Once);
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_ValidData_ReturnsOkWithToken()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "New User",
            Email = "newuser@example.com",
            Password = "Password123",
            Role = "Waiter",
            PhoneNumber = "+38761123456"
        };

        var expectedResponse = new AuthResponseDto
        {
            UserId = Guid.NewGuid(),
            Email = "newuser@example.com",
            FullName = "New User",
            Role = "Waiter",
            AccessToken = "fake-jwt-token",
            RefreshToken = "fake-refresh-token"
        };

        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(registerDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as AuthResponseDto;

        response.Should().NotBeNull();
        response!.Email.Should().Be("newuser@example.com");
        response.FullName.Should().Be("New User");
        response.Role.Should().Be("Waiter");

        _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<RegisterDto>()), Times.Once);
    }

    [Fact]
    public async Task Register_EmailAlreadyExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            FullName = "Test User",
            Email = "existing@example.com",
            Password = "Password123",
            Role = "Waiter"
        };

        _authServiceMock
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
            .ThrowsAsync(new InvalidOperationException("Email existing@example.com is already registered"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Register(registerDto));
        
        _authServiceMock.Verify(x => x.RegisterAsync(It.IsAny<RegisterDto>()), Times.Once);
    }

    #endregion

    #region ValidateToken Tests

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsTrue()
    {
        // Arrange
        var tokenDto = new TokenValidationDto { Token = "valid-jwt-token" };

        _authServiceMock
            .Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ValidateToken(tokenDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { isValid = true });

        _authServiceMock.Verify(x => x.ValidateTokenAsync(tokenDto.Token), Times.Once);
    }

    [Fact]
    public async Task ValidateToken_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var tokenDto = new TokenValidationDto { Token = "invalid-token" };

        _authServiceMock
            .Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ValidateToken(tokenDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { isValid = false });

        _authServiceMock.Verify(x => x.ValidateTokenAsync(tokenDto.Token), Times.Once);
    }

    #endregion

    #region RefreshToken Tests

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var refreshDto = new RefreshTokenDto { RefreshToken = "valid-refresh-token" };

        var expectedResponse = new AuthResponseDto
        {
            UserId = _testUserId,
            Email = "test@example.com",
            FullName = "Test User",
            Role = "Admin",
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token"
        };

        _authServiceMock
            .Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(refreshDto);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var response = okResult!.Value as AuthResponseDto;

        response.Should().NotBeNull();
        response!.AccessToken.Should().Be("new-access-token");
        response.RefreshToken.Should().Be("new-refresh-token");

        _authServiceMock.Verify(x => x.RefreshTokenAsync(refreshDto.RefreshToken), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_InvalidToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var refreshDto = new RefreshTokenDto { RefreshToken = "invalid-refresh-token" };

        _authServiceMock
            .Setup(x => x.RefreshTokenAsync(It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.RefreshToken(refreshDto));
        
        _authServiceMock.Verify(x => x.RefreshTokenAsync(refreshDto.RefreshToken), Times.Once);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_AuthenticatedUser_ReturnsNoContent()
    {
        // Arrange
        SetupAuthenticatedUser(_testUserId);

        _authServiceMock
            .Setup(x => x.LogoutAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _authServiceMock.Verify(x => x.LogoutAsync(_testUserId), Times.Once);
    }

    #endregion

    #region ChangePassword Tests

    [Fact]
    public async Task ChangePassword_ValidData_ReturnsNoContent()
    {
        // Arrange
        SetupAuthenticatedUser(_testUserId);

        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123"
        };

        _authServiceMock
            .Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(changePasswordDto);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _authServiceMock.Verify(x => x.ChangePasswordAsync(_testUserId, changePasswordDto), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_IncorrectCurrentPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        SetupAuthenticatedUser(_testUserId);

        var changePasswordDto = new ChangePasswordDto
        {
            CurrentPassword = "WrongPassword",
            NewPassword = "NewPassword123"
        };

        _authServiceMock
            .Setup(x => x.ChangePasswordAsync(It.IsAny<Guid>(), It.IsAny<ChangePasswordDto>()))
            .ThrowsAsync(new UnauthorizedAccessException("Current password is incorrect"));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            _controller.ChangePassword(changePasswordDto));

        _authServiceMock.Verify(x => x.ChangePasswordAsync(_testUserId, changePasswordDto), Times.Once);
    }

    #endregion

    #region GetCurrentUser Tests

    [Fact]
    public async Task GetCurrentUser_AuthenticatedUser_ReturnsUserDto()
    {
        // Arrange
        SetupAuthenticatedUser(_testUserId);

        var expectedUser = new UserDto
        {
            Id = _testUserId,
            FullName = "Test User",
            Email = "test@example.com",
            Role = "Admin",
            IsActive = true
        };

        _authServiceMock
            .Setup(x => x.GetCurrentUserAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var user = okResult!.Value as UserDto;

        user.Should().NotBeNull();
        user!.Id.Should().Be(_testUserId);
        user.Email.Should().Be("test@example.com");
        user.FullName.Should().Be("Test User");

        _authServiceMock.Verify(x => x.GetCurrentUserAsync(_testUserId), Times.Once);
    }

    #endregion

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsOkMessage()
    {
        // Arrange
        var email = "test@example.com";

        _authServiceMock
            .Setup(x => x.RequestPasswordResetAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ForgotPassword(email);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new 
        { 
            message = "If the email exists, a password reset link has been sent." 
        });

        _authServiceMock.Verify(x => x.RequestPasswordResetAsync(email), Times.Once);
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsSuccessMessage()
    {
        // Arrange
        var resetDto = new ResetPasswordDto
        {
            Token = "valid-reset-token",
            NewPassword = "NewPassword123"
        };

        _authServiceMock
            .Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordDto>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ResetPassword(resetDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(new { message = "Password reset successful" });

        _authServiceMock.Verify(x => x.ResetPasswordAsync(resetDto), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var resetDto = new ResetPasswordDto
        {
            Token = "invalid-token",
            NewPassword = "NewPassword123"
        };

        _authServiceMock
            .Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordDto>()))
            .ThrowsAsync(new InvalidOperationException("Invalid or expired reset token"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _controller.ResetPassword(resetDto));

        _authServiceMock.Verify(x => x.ResetPasswordAsync(resetDto), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
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
