using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class UserService(
    ApplicationDbContext context,
    ILogger<UserService> logger) : IUserService
{
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await context.Users
            .AsNoTracking()
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return users;
    }

    public async Task<UserDto> GetUserByIdAsync(Guid id)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (user == null)
            throw new KeyNotFoundException($"User with ID {id} not found");

        return user;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        // Validate email uniqueness
        if (await context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException($"Email {dto.Email} is already registered");

        // Validate role
        if (!Enum.TryParse<UserRole>(dto.Role, out var role))
            throw new InvalidOperationException($"Invalid role: {dto.Role}");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role,
            PhoneNumber = dto.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} created: {Email} with role {Role}", 
            user.Id, user.Email, user.Role);

        return await GetUserByIdAsync(user.Id);
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {id} not found");

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} updated", id);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await context.Users
            .Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            throw new KeyNotFoundException($"User with ID {id} not found");

        // Check if user has orders (soft delete instead)
        if (user.Orders.Any())
        {
            logger.LogWarning("User {UserId} has {OrderCount} orders. Performing soft delete (deactivate) instead.", 
                id, user.Orders.Count);
            
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            logger.LogInformation("User {UserId} deactivated (soft delete)", id);
            return;
        }

        // Hard delete if no orders
        context.Users.Remove(user);
        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} permanently deleted", id);
    }

    public async Task<List<UserDto>> GetUsersByRoleAsync(UserRole role)
    {
        var users = await context.Users
            .AsNoTracking()
            .Where(u => u.Role == role && u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        logger.LogInformation("Retrieved {Count} users with role {Role}", users.Count, role);

        return users;
    }

    public async Task DeactivateUserAsync(Guid id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {id} not found");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} ({Email}) deactivated", id, user.Email);
    }

    public async Task ActivateUserAsync(Guid id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {id} not found");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("User {UserId} ({Email}) activated", id, user.Email);
    }

    public async Task<List<UserDto>> GetActiveUsersAsync()
    {
        var users = await context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return users;
    }
}
