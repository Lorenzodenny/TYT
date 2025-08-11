using System.Security.Claims;
using TYT.Models;

namespace TYT.Services.Security;

public interface IJwtClaimsFactory
{
    /// <summary>
    /// Crea i claims standard + ruoli custom per l'utente.
    /// </summary>
    Task<List<Claim>> BuildAsync(TYTUser user, bool includeJti, CancellationToken ct);
}
