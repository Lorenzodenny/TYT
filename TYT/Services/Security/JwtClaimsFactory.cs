using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TYT.Models;
using static TYT.Shared.Enums;

namespace TYT.Services.Security;

public sealed class JwtClaimsFactory(
    IRoleClaimsService roleClaimsService
) : IJwtClaimsFactory
{
    private const string RoleType = nameof(TYTRole);

    public async Task<List<Claim>> BuildAsync(TYTUser user, bool includeJti, CancellationToken ct)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        if (includeJti)
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

        // Ruoli come claims custom (coerente con il tuo RoleClaimsService)
        var roles = await roleClaimsService.GetRolesAsync(user.Id, ct);
        claims.AddRange(roles.Select(r => new Claim(RoleType, r)));

        return claims;
    }
}
