using OrdersAPI.Domain.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class AuthService(
    ApplicationDbContext context,
    IConfiguration configuration,
    ILogger<AuthService> logger,
    IEmailSender emailSender,
    ITokenBlacklistService tokenBlacklist,
    IHttpContextAccessor httpContextAccessor)
    : IAuthService
{
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            logger.LogWarning("Failed login attempt for email: {Email}", dto.Email);
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Inactive user attempted login: {Email}", dto.Email);
            throw new UnauthorizedAccessException("User account is inactive");
        }

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateSecureToken();

        await PersistRefreshTokenAsync(user.Id, refreshToken);

        logger.LogInformation("User {Email} logged in successfully", user.Email);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            logger.LogWarning("Registration attempt with existing email: {Email}", dto.Email);
            throw new ConflictException("Email already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Waiter,
            PhoneNumber = dto.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateSecureToken();

        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            context.Users.Add(user);
            context.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = HashToken(refreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        logger.LogInformation("User {Email} registered successfully", user.Email);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);

        var storedToken = await context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (storedToken == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        if (storedToken.IsRevoked)
            throw new UnauthorizedAccessException("Refresh token has been revoked");

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired");

        if (!storedToken.User.IsActive)
            throw new UnauthorizedAccessException("User account is inactive");

        // Revoke old token (rotation — old token cannot be reused)
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        // Issue new tokens
        var newAccessToken = GenerateJwtToken(storedToken.User);
        var newRefreshToken = GenerateSecureToken();
        var newHash = HashToken(newRefreshToken);

        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        logger.LogInformation("Refresh token rotated for user {UserId}", storedToken.UserId);

        return new AuthResponseDto
        {
            UserId = storedToken.User.Id,
            Email = storedToken.User.Email,
            FullName = storedToken.User.FullName,
            Role = storedToken.User.Role.ToString(),
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException($"User with ID {userId} not found");

        // Extract jti and expiry from the current request's token via IHttpContextAccessor
        var principal = httpContextAccessor.HttpContext?.User;
        var jti = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expClaim = principal?.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (jti != null)
        {
            var expiry = expClaim != null
                ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim)).UtcDateTime
                : DateTime.UtcNow.AddHours(24);
            tokenBlacklist.Revoke(jti, expiry);
        }

        // Revoke all active refresh tokens
        var activeTokens = await context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        if (activeTokens.Count > 0)
            await context.SaveChangesAsync();

        logger.LogInformation("User {Email} logged out, access token blacklisted, {Count} refresh token(s) revoked",
            user.Email, activeTokens.Count);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException($"User with ID {userId} not found");

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            logger.LogWarning("Failed password change attempt for user {UserId}", userId);
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("Password changed successfully for user {UserId}", userId);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException($"User with ID {userId} not found");

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.ToString(),
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            throw new NotFoundException($"User with ID {userId} not found");

        if (dto.Email != user.Email && await context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId))
            throw new ConflictException($"Email {dto.Email} is already in use");

        user.FullName = dto.FullName;
        user.Email = dto.Email.ToLower().Trim();
        user.PhoneNumber = dto.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} updated their profile", userId);
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            // Do not reveal whether the email is registered (prevents enumeration)
            logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return;
        }

        // Invalidate any existing unused reset tokens for this user
        var existingTokens = await context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
        foreach (var t in existingTokens)
            t.IsUsed = true;

        // Generate and persist new one-time reset token
        var plainToken = GenerateSecureToken();
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(plainToken),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        await emailSender.SendAsync(
            user.Email,
            "Password Reset Request – OrderS",
            $"<p>Hello {user.FullName},</p>" +
            $"<p>Your password reset token is:</p>" +
            $"<p><strong>{plainToken}</strong></p>" +
            $"<p>This token expires in <strong>1 hour</strong> and can only be used once.</p>" +
            $"<p>If you did not request a password reset, ignore this email.</p>");

        logger.LogInformation("Password reset token sent to {Email}", email);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid reset token");

        var tokenHash = HashToken(dto.Token);
        var resetToken = await context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.UserId == user.Id && t.TokenHash == tokenHash);

        if (resetToken == null)
            throw new UnauthorizedAccessException("Invalid reset token");

        if (resetToken.IsUsed)
            throw new UnauthorizedAccessException("Reset token has already been used");

        if (resetToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Reset token has expired");

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Mark token as used (one-time use)
        resetToken.IsUsed = true;

        // Force re-login on all devices by revoking all refresh tokens
        var activeRefreshTokens = await context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync();
        foreach (var rt in activeRefreshTokens)
        {
            rt.IsRevoked = true;
            rt.RevokedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        logger.LogInformation("Password reset successfully for user {Email}", dto.Email);
    }

    // ==================== PRIVATE HELPERS ====================

    private async Task PersistRefreshTokenAsync(Guid userId, string plainToken)
    {
        context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(plainToken),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    private string GenerateJwtToken(User user)
    {
        var key = configuration["Jwt:Key"] ?? throw new BusinessException("JWT key not configured");
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
