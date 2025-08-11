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
    SignInManager<TYTUser> signInManager,
    IHttpContextAccessor accessor
) : IQueryHandler<MeQuery, MeResponse>
{
    public async Task<MeResponse> Handle(MeQuery req, CancellationToken ct)
    {
        var http = accessor.HttpContext!;
        var principal = http.User;

        if (!(principal?.Identity?.IsAuthenticated ?? false))
            throw new UnauthorizedAccessException();

        // Rinnova/estende il cookie (keep-alive)
        var user = await userManager.GetUserAsync(principal);
        if (user is not null)
            await signInManager.RefreshSignInAsync(user);

        // Leggi i claim dal principal (include "TYTRole")
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = principal.Identity?.Name;
        var email = principal.FindFirstValue(ClaimTypes.Email);

        const string roleType = nameof(TYT.Shared.Enums.TYTRole);
        var roles = principal.Claims
            .Where(c => c.Type == roleType)
            .Select(c => c.Value)
            .Distinct()
            .ToArray();

        return new MeResponse(true, userId, name, email, roles);
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
