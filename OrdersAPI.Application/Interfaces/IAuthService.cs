using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<bool> ValidateTokenAsync(string token);
    
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(Guid userId);
    Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    Task<UserDto> GetCurrentUserAsync(Guid userId);
    Task RequestPasswordResetAsync(string email);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}
