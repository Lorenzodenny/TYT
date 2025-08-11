using System.Security.Claims;
using TYT.Models;

namespace TYT.Services.Auth;

public interface IJwtTokenService
{
    Task<(string accessToken, DateTime expiresUtc)> CreateAccessTokenAsync(TYTUser user, IEnumerable<Claim> claims, CancellationToken ct);
    Task<(string refreshToken, DateTime expiresUtc)> CreateAndStoreRefreshTokenAsync(string userId, CancellationToken ct);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct);
    Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct);
}