using Microsoft.Extensions.Caching.Memory;
using OrdersAPI.Application.Interfaces;

namespace OrdersAPI.Infrastructure.Services;

public class TokenBlacklistService(IMemoryCache cache) : ITokenBlacklistService
{
    public void Revoke(string jti, DateTime expiry)
    {
        var ttl = expiry - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
            cache.Set($"blacklist:{jti}", true, ttl);
    }

    public bool IsRevoked(string jti) =>
        cache.TryGetValue($"blacklist:{jti}", out _);
}
