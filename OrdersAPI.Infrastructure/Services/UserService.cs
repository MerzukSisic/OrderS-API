using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrdersAPI.Application.DTOs;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Infrastructure.Data;

namespace OrdersAPI.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(ApplicationDbContext context, IMapper mapper, ILogger<UserService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users.ToListAsync();
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<UserDto> GetUserByIdAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User {id} not found");

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            throw new InvalidOperationException("Email already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = Enum.Parse<UserRole>(dto.Role),
            PhoneNumber = dto.PhoneNumber,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created: {Email}", user.Id, user.Email);

        return _mapper.Map<UserDto>(user);
    }

    public async Task UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User {id} not found");

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;
        if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _logger.LogInformation("User {UserId} updated", id);
    }

    public async Task DeleteUserAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            throw new KeyNotFoundException($"User {id} not found");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted", id);
    }
}

