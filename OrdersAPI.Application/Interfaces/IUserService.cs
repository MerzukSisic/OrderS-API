using OrdersAPI.Application.DTOs;
using OrdersAPI.Domain.Entities;
using OrdersAPI.Domain.Enums;

namespace OrdersAPI.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto> GetUserByIdAsync(Guid id);
    Task<UserDto> CreateUserAsync(CreateUserDto dto);
    Task UpdateUserAsync(Guid id, UpdateUserDto dto);
    Task DeleteUserAsync(Guid id);
    
    Task<List<UserDto>> GetUsersByRoleAsync(UserRole role);
    Task DeactivateUserAsync(Guid id);
    Task ActivateUserAsync(Guid id);
    Task<List<UserDto>> GetActiveUsersAsync();
}
