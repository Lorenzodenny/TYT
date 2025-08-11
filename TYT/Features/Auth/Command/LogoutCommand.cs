using System.Security.Claims;
using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TYT.Dispatcher.Interface;
using TYT.Data;    

namespace TYT.Features.Auth.Command
{
    // Command 
    public sealed record LogoutCommand() : ICommand<LogoutResponse>;

    // Response DTO
    public sealed record LogoutResponse(bool Success, string Message);

    // Validator
    public sealed class LogoutValidator : AbstractValidator<LogoutCommand>
    {
        public LogoutValidator(IHttpContextAccessor http)
        {
            RuleFor(_ => _)
                .Must(_ => http.HttpContext?.User?.Identity?.IsAuthenticated == true)
                .WithMessage("Utente non autenticato.");
        }
    }

    // Handler (con ExecuteUpdateAsync)
    public sealed class LogoutHandler : ICommandHandler<LogoutCommand, LogoutResponse>
    {
        private readonly TYTDbContext _db;
        private readonly IHttpContextAccessor _http;

        public LogoutHandler(TYTDbContext db, IHttpContextAccessor http)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<LogoutResponse> Handle(LogoutCommand req, CancellationToken ct)
        {
            var user = _http.HttpContext?.User;
            if (user is null || user.Identity is null || !user.Identity.IsAuthenticated)
                return new(false, "Utente non autenticato.");

            // id utente dal JWT (NameIdentifier o 'sub')
            var userId =
                user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                user.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return new(false, "Impossibile determinare l'utente dal token.");

            var now = DateTime.UtcNow;

            // Revoca in UNA query (idempotente): set RevokedAtUtc = now
            // e forza la scadenza se futura
            var affected = await _db.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(rt => rt.RevokedAtUtc, now)
                        .SetProperty(rt => rt.ExpiresAtUtc, rt => rt.ExpiresAtUtc > now ? now : rt.ExpiresAtUtc),
                    ct);

            return new(true, "Logout effettuato");
        }
    }

    // Endpoint (Carter)
    public sealed class LogoutEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/api/auth/logout", async (
                [FromServices] ISender sender,
                CancellationToken ct) =>
            {
                var res = await sender.Send(new LogoutCommand(), ct);
                return Results.Ok(new { success = res.Success, message = res.Message });
            })
            .WithTags("Auth")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK);
        }
    }
}
