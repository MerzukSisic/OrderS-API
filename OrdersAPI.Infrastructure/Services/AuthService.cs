using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
    ILogger<AuthService> logger)
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

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        logger.LogInformation("User {Email} logged in successfully", user.Email);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            AccessToken = token,
            RefreshToken = refreshToken
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            logger.LogWarning("Registration attempt with existing email: {Email}", dto.Email);
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = Enum.Parse<UserRole>(dto.Role),
            PhoneNumber = dto.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        logger.LogInformation("User {Email} registered successfully", user.Email);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            AccessToken = token,
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
        logger.LogInformation("Refresh token requested");
        
        throw new NotImplementedException("Refresh token functionality requires database storage. Implement RefreshToken entity first.");
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

        logger.LogInformation("User {Email} logged out", user.Email);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {userId} not found");

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
            throw new KeyNotFoundException($"User with ID {userId} not found");

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

    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return;
        }

        logger.LogInformation("Password reset requested for user {Email}", email);
        
        throw new NotImplementedException("Password reset requires email service implementation and token storage.");
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            throw new KeyNotFoundException("Invalid reset token");

        logger.LogInformation("Password reset for user {Email}", dto.Email);
        
        throw new NotImplementedException("Password reset requires token validation implementation.");
    }

    private string GenerateJwtToken(User user)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured");
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
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

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}
