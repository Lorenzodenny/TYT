using Carter;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TYT.Dispatcher.Interface;
using TYT.Models;

namespace TYT.Features.Auth.Command;

// Command ( Request )
public sealed record ConfirmEmailCommand(string UserId, string Token) : ICommand<ConfirmEmailResponse>;

// Handle
public sealed class ConfirmEmailHandler(UserManager<TYTUser> userManager)
    : ICommandHandler<ConfirmEmailCommand, ConfirmEmailResponse>
{
    public async Task<ConfirmEmailResponse> Handle(ConfirmEmailCommand req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.UserId);
        if (user is null) return new(false, "Utente non trovato.");

        // Il token è stato URL-encoded nella mail: decodifichiamolo
        var token = Uri.UnescapeDataString(req.Token);

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            var msg = string.Join("; ", result.Errors.Select(e => e.Description));
            return new(false, $"Conferma email fallita: {msg}");
        }

        return new(true, "Email confermata con successo.");
    }
}

// Response DTO
public sealed record ConfirmEmailResponse(
    bool Success, 
    string Message
    );

// Endpoint
public sealed class ConfirmEmailEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/confirm-email", async (
            [FromQuery] string userId,
            [FromQuery] string token,
            [FromServices] ISender sender,
            CancellationToken ct) =>
        {
            var res = await sender.Send(new ConfirmEmailCommand(userId, token), ct);
            return res.Success ? Results.Ok(res) : Results.BadRequest(res);
        })
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
