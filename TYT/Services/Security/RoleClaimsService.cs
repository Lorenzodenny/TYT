using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TYT.Data;
using TYT.Models;
using TYT.Shared;
using static TYT.Shared.Enums;

namespace TYT.Services.Security;

public sealed class RoleClaimsService(
    UserManager<TYTUser> userManager,
    TYTDbContext context) : IRoleClaimsService
{
    private const string RoleType = nameof(Enums.TYTRole);

    public async Task SetRoleAsync(TYTUser user, TYTRole role, CancellationToken ct)
    {
        var claims = await userManager.GetClaimsAsync(user);
        foreach (var c in claims.Where(c => c.Type == RoleType))
            await userManager.RemoveClaimAsync(user, c);

        var add = await userManager.AddClaimAsync(user, new Claim(RoleType, role.ToString()));
        if (!add.Succeeded)
            throw new InvalidOperationException(string.Join("; ", add.Errors.Select(e => $"{e.Code}: {e.Description}")));
    }

    public async Task<string[]> GetRolesAsync(string userId, CancellationToken ct)
    {
        return await context.Set<IdentityUserClaim<string>>()
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.ClaimType == RoleType)
            .Select(c => c.ClaimValue!)
            .Distinct()
            .ToArrayAsync(ct);
    }

    public async Task<Dictionary<string, string[]>> GetRolesAsync(IEnumerable<string> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToArray();
        var map = await context.Set<IdentityUserClaim<string>>()
            .AsNoTracking()
            .Where(c => ids.Contains(c.UserId) && c.ClaimType == RoleType)
            .GroupBy(c => c.UserId)
            .Select(g => new { UserId = g.Key, Roles = g.Select(x => x.ClaimValue!).Distinct().ToArray() })
            .ToDictionaryAsync(x => x.UserId, x => x.Roles, ct);

        // Assicura chiave per tutti gli utenti richiesti
        foreach (var id in ids)
            map.TryAdd(id, Array.Empty<string>());

        return map;
    }
}
