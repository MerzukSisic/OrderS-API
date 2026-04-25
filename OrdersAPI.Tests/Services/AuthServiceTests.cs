using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;
using OrdersAPI.Infrastructure.Services;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace OrdersAPI.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly AuthService _service;
    private readonly Mock<IEmailSender> _emailMock;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "TestSuperSecretKeyForJwtTokenGenerationThatIsAtLeast32Chars",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            })
            .Build();

        _emailMock = new Mock<IEmailSender>();
        _emailMock
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var logger = Mock.Of<ILogger<AuthService>>();
        _service = new AuthService(_db, config, logger, _emailMock.Object);
    }

    public void Dispose() => _db.Dispose();

    // ==================== HELPERS ====================

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private async Task<User> CreateTestUserAsync(string email = "test@orders.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Waiter,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<(User user, string plainToken)> CreateTestUserWithRefreshTokenAsync(
        DateTime? expiresAt = null, bool isRevoked = false)
    {
        var user = await CreateTestUserAsync();
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        var plain = Convert.ToBase64String(bytes);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(plain),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30),
            IsRevoked = isRevoked,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return (user, plain);
    }

    // ==================== REFRESH TOKEN TESTS ====================

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewTokensAndRevokesOld()
    {
        var (user, plainToken) = await CreateTestUserWithRefreshTokenAsync();

        var result = await _service.RefreshTokenAsync(plainToken);

        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(plainToken, "token rotation must issue a new token");
        result.Email.Should().Be(user.Email);

        // Old token must be revoked
        var oldHash = HashToken(plainToken);
        var oldToken = await _db.RefreshTokens.FirstAsync(t => t.TokenHash == oldHash);
        oldToken.IsRevoked.Should().BeTrue("old token must be revoked after rotation");
        oldToken.RevokedAt.Should().NotBeNull();

        // New token must be persisted and valid
        var newHash = HashToken(result.RefreshToken!);
        var newToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == newHash);
        newToken.Should().NotBeNull("new refresh token must be persisted in DB");
        newToken!.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ThrowsUnauthorized()
    {
        var (_, plainToken) = await CreateTestUserWithRefreshTokenAsync(isRevoked: true);

        var act = () => _service.RefreshTokenAsync(plainToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*revoked*");
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ThrowsUnauthorized()
    {
        var (_, plainToken) = await CreateTestUserWithRefreshTokenAsync(
            expiresAt: DateTime.UtcNow.AddSeconds(-1));

        var act = () => _service.RefreshTokenAsync(plainToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidToken_ThrowsUnauthorized()
    {
        var act = () => _service.RefreshTokenAsync("completely_invalid_token_that_does_not_exist");

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid*");
    }

    [Fact]
    public async Task RefreshTokenAsync_RotatedTokenCannotBeReused()
    {
        var (_, plainToken) = await CreateTestUserWithRefreshTokenAsync();

        // First rotation succeeds
        var first = await _service.RefreshTokenAsync(plainToken);
        first.Should().NotBeNull();

        // Re-using the original (now revoked) token must fail
        var act = () => _service.RefreshTokenAsync(plainToken);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*revoked*");
    }

    // ==================== FORGOT/RESET PASSWORD TESTS ====================

    [Fact]
    public async Task RequestPasswordResetAsync_ValidEmail_PersistsTokenAndSendsEmail()
    {
        var user = await CreateTestUserAsync("reset@orders.com");

        await _service.RequestPasswordResetAsync(user.Email);

        // Token was persisted
        var token = await _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.UserId == user.Id);
        token.Should().NotBeNull();
        token!.IsUsed.Should().BeFalse();
        token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        // Email was sent
        _emailMock.Verify(x => x.SendAsync(
            user.Email,
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_NonExistentEmail_SilentlySucceeds()
    {
        // Must not throw (prevents email enumeration)
        var act = () => _service.RequestPasswordResetAsync("ghost@orders.com");
        await act.Should().NotThrowAsync();

        // No email sent
        _emailMock.Verify(x => x.SendAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndRevokesRefreshTokens()
    {
        var user = await CreateTestUserAsync("reset2@orders.com");

        // Create a refresh token for the user so we can verify it gets revoked
        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, TokenHash = "some_hash",
            ExpiresAt = DateTime.UtcNow.AddDays(30), IsRevoked = false, CreatedAt = DateTime.UtcNow
        });

        // Generate a valid reset token directly (bypassing email)
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        var plainResetToken = Convert.ToBase64String(bytes);
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(), UserId = user.Id,
            TokenHash = HashToken(plainResetToken),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var dto = new ResetPasswordDto
        {
            Email = user.Email,
            Token = plainResetToken,
            NewPassword = "NewSecurePassword456!"
        };

        await _service.ResetPasswordAsync(dto);

        // Password changed
        var updated = await _db.Users.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify("NewSecurePassword456!", updated!.PasswordHash).Should().BeTrue();

        // Reset token marked as used
        var resetToken = await _db.PasswordResetTokens.FirstAsync(t => t.UserId == user.Id);
        resetToken.IsUsed.Should().BeTrue();

        // Refresh tokens revoked
        var refreshTokens = await _db.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        refreshTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public async Task ResetPasswordAsync_AlreadyUsedToken_ThrowsUnauthorized()
    {
        var user = await CreateTestUserAsync("reset3@orders.com");

        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        var plainToken = Convert.ToBase64String(bytes);
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(), UserId = user.Id,
            TokenHash = HashToken(plainToken),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true, // already used
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var act = () => _service.ResetPasswordAsync(new ResetPasswordDto
        {
            Email = user.Email, Token = plainToken, NewPassword = "NewPass123!"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*already been used*");
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ThrowsUnauthorized()
    {
        var user = await CreateTestUserAsync("reset4@orders.com");

        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        var plainToken = Convert.ToBase64String(bytes);
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(), UserId = user.Id,
            TokenHash = HashToken(plainToken),
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1), // expired
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var act = () => _service.ResetPasswordAsync(new ResetPasswordDto
        {
            Email = user.Email, Token = plainToken, NewPassword = "NewPass123!"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*expired*");
    }

    // ==================== LOGIN PERSISTS REFRESH TOKEN ====================

    [Fact]
    public async Task LoginAsync_ValidCredentials_PersistsRefreshTokenInDb()
    {
        var user = await CreateTestUserAsync("login@orders.com");
        // re-hash since CreateTestUserAsync already saves
        var plainPassword = "Password123!";
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        await _db.SaveChangesAsync();

        var result = await _service.LoginAsync(new LoginDto { Email = user.Email, Password = plainPassword });

        result.RefreshToken.Should().NotBeNullOrEmpty();

        var hash = HashToken(result.RefreshToken!);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash);
        stored.Should().NotBeNull("refresh token must be persisted on login");
        stored!.UserId.Should().Be(user.Id);
        stored.IsRevoked.Should().BeFalse();
    }
}
