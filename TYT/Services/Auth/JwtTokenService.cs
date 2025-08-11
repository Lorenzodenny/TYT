using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TYT.Data;
using TYT.Models;

namespace TYT.Services.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opt;
    private readonly TYTDbContext _db;

    public JwtTokenService(IOptions<JwtOptions> opt, TYTDbContext db)
    {
        _opt = opt.Value;
        _db = db;
    }

    public async Task<(string accessToken, DateTime expiresUtc)> CreateAccessTokenAsync(TYTUser user, IEnumerable<Claim> claims, CancellationToken ct)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_opt.AccessTokenMinutes);

        var jwt = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (token, expires);
    }

    public async Task<(string refreshToken, DateTime expiresUtc)> CreateAndStoreRefreshTokenAsync(string userId, CancellationToken ct)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expires = DateTime.UtcNow.AddDays(_opt.RefreshTokenDays);

        var entity = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAtUtc = expires
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);

        return (token, expires);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken, ct);
        if (rt is null) return false;
        if (rt.RevokedAtUtc.HasValue) return true;
        rt.RevokedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var rt = await _db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(x => x.Token == refreshToken, ct);
        if (rt is null) return null;
        if (rt.IsExpired || rt.IsRevoked) return null;
        return rt;
    }
}