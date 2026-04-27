namespace OrdersAPI.Application.Interfaces;

public interface ITokenBlacklistService
{
    void Revoke(string jti, DateTime expiry);
    bool IsRevoked(string jti);
}
