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
public sealed record GetUserByIdQuery(string Id) : IQuery<GetUserByIdResponse>;

// Handler
public sealed class GetUserByIdHandler(
    TYTDbContext context,
    IRoleClaimsService roles)
    : IQueryHandler<GetUserByIdQuery, GetUserByIdResponse>
{
    public async Task<GetUserByIdResponse> Handle(GetUserByIdQuery req, CancellationToken ct)
    {
        var u = await context.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
                ?? throw new KeyNotFoundException("Utente non trovato.");

        var rolesArr = await roles.GetRolesAsync(u.Id, ct);
        return new GetUserByIdResponse(u.Id, u.Email!, u.Nome, u.Cognome, u.IsDeleted, rolesArr);

    }
}

// Response
public sealed record GetUserByIdResponse(string Id, string Email, string? Nome, string? Cognome, bool IsDeleted, string[] Roles);


// Endpoint
public sealed class GetUserByIdEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/getbyid-user/{id}", async (
            [FromRoute] string id,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var res = await sender.Send(new GetUserByIdQuery(id), ct);
                return Results.Ok(res);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
        })
        .WithTags("Users")
        .RequireAuthorization(PolicyNames.AnyAuthenticated);
    }
}
