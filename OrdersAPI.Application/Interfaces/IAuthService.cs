using OrdersAPI.Application.DTOs;

namespace OrdersAPI.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<bool> ValidateTokenAsync(string token);
}
