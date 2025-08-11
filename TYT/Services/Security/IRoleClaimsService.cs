using TYT.Models;
using static TYT.Shared.Enums;

namespace TYT.Services.Security
{
    public interface IRoleClaimsService
    {
        Task SetRoleAsync(TYTUser user, TYTRole role, CancellationToken ct);
        Task<string[]> GetRolesAsync(string userId, CancellationToken ct);
        Task<Dictionary<string, string[]>> GetRolesAsync(IEnumerable<string> userIds, CancellationToken ct);
    }
}
