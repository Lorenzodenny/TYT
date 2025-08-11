using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TYT.Data;
using TYT.Dispatcher.Interface;
using TYT.Services.Security;
using TYT.Shared;

namespace TYT.Features.Users.Query;

// Query
public sealed record GetAllUsersQuery() : IQuery<List<GetAllUsersResponse>>;

// Handler
public sealed class GetAllUsersHandler(
    TYTDbContext context,
    IRoleClaimsService roles)
    : IQueryHandler<GetAllUsersQuery, List<GetAllUsersResponse>>
{
    public async Task<List<GetAllUsersResponse>> Handle(GetAllUsersQuery req, CancellationToken ct)
    {
        var users = await context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted)
            .Select(u => new { u.Id, u.Email, u.Nome, u.Cognome, u.IsDeleted })
            .ToListAsync(ct);

        var ids = users.Select(u => u.Id).ToList();

        const string roleType = nameof(Enums.TYTRole);

        var rolesMap = await roles.GetRolesAsync(ids, ct);

        var list = users.Select(u =>
        {
            return new GetAllUsersResponse(
                u.Id,
                u.Email ?? string.Empty,
                u.Nome,
                u.Cognome,
                u.IsDeleted,
                rolesMap[u.Id]
            );
        }).ToList();

        return list;
    }
}

// Response (singolo elemento della lista)
public sealed record GetAllUsersResponse(
    string Id,
    string Email,
    string? Nome,
    string? Cognome,
    bool IsDeleted,
    string[] Roles
);

// Endpoint
public sealed class GetAllUsersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/getall-users", async (
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var res = await sender.Send(new GetAllUsersQuery(), ct);
            return Results.Ok(res);
        })
        .WithTags("Users")
        .RequireAuthorization(PolicyNames.AnyAuthenticated);
    }
}
