using Carter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TYT.Dispatcher.Interface;
using TYT.Models;

namespace TYT.Features.Auth.Command;

// Command (senza body)
public sealed record LogoutCommand() : ICommand<LogoutResponse>;

public sealed record LogoutResponse(bool Success, string Message);

// Handler
public sealed class LogoutHandler(SignInManager<TYTUser> signInManager)
    : ICommandHandler<LogoutCommand, LogoutResponse>
{
    public async Task<LogoutResponse> Handle(LogoutCommand req, CancellationToken ct)
    {
        await signInManager.SignOutAsync();
        return new(true, "Logout effettuato.");
    }
}

// Endpoint
public sealed class LogoutEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout", async (
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var res = await sender.Send(new LogoutCommand(), ct);
            return Results.Ok(res);
        })
        .WithTags("Auth")
        .RequireAuthorization();
    }
}
