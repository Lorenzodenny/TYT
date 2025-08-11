using System.Security.Claims;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TYT.Dispatcher.Interface;

namespace TYT.Features.Auth.Query;

// Query (senza parametri)
public sealed record GetMyClaimsQuery() : IQuery<GetMyClaimsResponse>;

// DTO di risposta
public sealed record ClaimItem(string Type, string Value);
public sealed record GetMyClaimsResponse(
    bool IsAuthenticated,
    string? UserId,
    string? Name,
    string? Email,
    string[] Roles,
    IReadOnlyList<ClaimItem> Claims
);

// Handler (legge da HttpContext.User)
public sealed class GetMyClaimsHandler(IHttpContextAccessor ctxAccessor)
    : IQueryHandler<GetMyClaimsQuery, GetMyClaimsResponse>
{
    public Task<GetMyClaimsResponse> Handle(GetMyClaimsQuery req, CancellationToken ct)
    {
        var user = ctxAccessor.HttpContext?.User ?? new ClaimsPrincipal();
        var isAuth = user.Identity?.IsAuthenticated ?? false;

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = user.Identity?.Name;
        var email = user.FindFirstValue(ClaimTypes.Email);

        // I ruoli che salvi tu: claim type = "TYTRole"
        const string roleType = nameof(TYT.Shared.Enums.TYTRole);
        var roles = user.Claims
            .Where(c => c.Type == roleType)
            .Select(c => c.Value)
            .Distinct()
            .ToArray();

        var all = user.Claims.Select(c => new ClaimItem(c.Type, c.Value)).ToList();

        return Task.FromResult(new GetMyClaimsResponse(isAuth, userId, name, email, roles, all));
    }
}

// Endpoint
public sealed class GetMyClaimsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/claims", async (
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var res = await sender.Send(new GetMyClaimsQuery(), ct);
            return Results.Ok(res);
        })
        .WithTags("Auth")
        .RequireAuthorization();
    }
}
