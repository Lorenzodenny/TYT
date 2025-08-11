using System.Security.Claims;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TYT.Dispatcher.Interface;
using TYT.Models;

namespace TYT.Features.Auth.Query;

public sealed record MeQuery() : IQuery<MeResponse>;

public sealed record MeResponse(
    bool IsAuthenticated,
    string? UserId,
    string? Name,
    string? Email,
    string[] Roles
);

public sealed class MeHandler(
    UserManager<TYTUser> userManager,
    IHttpContextAccessor accessor
) : IQueryHandler<MeQuery, MeResponse>
{
    public async Task<MeResponse> Handle(MeQuery req, CancellationToken ct)
    {
        var http = accessor.HttpContext!;
        var principal = http.User;

        if (!(principal?.Identity?.IsAuthenticated ?? false))
            throw new UnauthorizedAccessException();

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var name = principal.Identity?.Name;

        var user = await userManager.FindByIdAsync(userId!);

        // Recupera ruoli come prima (claims TYTRole già assegnati lato creazione utente)
        var roles = principal.Claims
            .Where(c => c.Type.EndsWith("TYTRole") || c.Type.Contains("TYTRole"))
            .Select(c => c.Value)
            .Distinct()
            .ToArray();

        return new(true, user!.Id, email, name, roles);
    }
}

public sealed class MeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", async (
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var res = await sender.Send(new MeQuery(), ct);
                return Results.Ok(res);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        })
        .WithTags("Auth")
        .RequireAuthorization();
    }
}
