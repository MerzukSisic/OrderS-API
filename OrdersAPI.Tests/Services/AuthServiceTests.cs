using FluentAssertions;
using Microsoft.AspNetCore.Http;
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

        var logger = Mock.Of<ILogger<AuthService>>();
        var blacklist = Mock.Of<ITokenBlacklistService>();
        var httpContextAccessor = Mock.Of<IHttpContextAccessor>();
        _service = new AuthService(_db, config, logger, blacklist, httpContextAccessor);
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

    // ==================== RESET PASSWORD TESTS ====================

    [Fact]
    public async Task ResetPasswordAsync_ValidEmail_UpdatesPasswordAndRevokesRefreshTokens()
    {
        var user = await CreateTestUserAsync("reset@orders.com");

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, TokenHash = "some_hash",
            ExpiresAt = DateTime.UtcNow.AddDays(30), IsRevoked = false, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var dto = new ResetPasswordDto { Email = user.Email, NewPassword = "NewSecurePassword456!" };
        await _service.ResetPasswordAsync(dto);

        var updated = await _db.Users.FindAsync(user.Id);
        BCrypt.Net.BCrypt.Verify("NewSecurePassword456!", updated!.PasswordHash).Should().BeTrue();

        var refreshTokens = await _db.RefreshTokens.Where(t => t.UserId == user.Id).ToListAsync();
        refreshTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());
    }

    [Fact]
    public async Task ResetPasswordAsync_NonExistentEmail_ThrowsUnauthorized()
    {
        var act = () => _service.ResetPasswordAsync(
            new ResetPasswordDto { Email = "ghost@orders.com", NewPassword = "NewPass123!" });

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
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
