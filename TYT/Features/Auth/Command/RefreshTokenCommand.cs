using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using TYT.Dispatcher.Interface;
using TYT.Models;
using TYT.Services.Auth;
using TYT.Services.Security;

namespace TYT.Features.Auth.Command;

public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResponse>;

public sealed record RefreshTokenResponse(
    bool Success,
    string Message,
    string? AccessToken,
    DateTime? AccessTokenExpiresUtc,
    string? NewRefreshToken,
    DateTime? RefreshTokenExpiresUtc
);

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenHandler(
    UserManager<TYTUser> userManager,
    IJwtTokenService tokenService,
    IJwtClaimsFactory claimsFactory
) : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand req, CancellationToken ct)
    {
        var rt = await tokenService.ValidateRefreshTokenAsync(req.RefreshToken, ct);
        if (rt is null)
            return new(false, "Refresh token non valido", null, null, null, null);

        var user = await userManager.FindByIdAsync(rt.UserId);
        if (user is null)
            return new(false, "Utente non trovato", null, null, null, null);

        // Claims (standard + ruoli) con jti
        var claims = await claimsFactory.BuildAsync(user, includeJti: true, ct);

        // Nuovo access token
        var (access, accessExp) = await tokenService.CreateAccessTokenAsync(user, claims, ct);

        // Rotazione refresh: revoca quello usato e genera un nuovo token
        await tokenService.RevokeRefreshTokenAsync(req.RefreshToken, ct);
        var (newRefresh, newRefreshExp) = await tokenService.CreateAndStoreRefreshTokenAsync(user.Id, ct);

        return new(true, "Token rinnovato", access, accessExp, newRefresh, newRefreshExp);
    }
}

public sealed class RefreshTokenEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh-token", async (
            [FromBody] RefreshTokenCommand cmd,
            [FromServices] ISender sender,
            [FromServices] IValidator<RefreshTokenCommand> validator,
            CancellationToken ct) =>
        {
            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());

            var res = await sender.Send(cmd, ct);
            return res.Success ? Results.Ok(res) : Results.BadRequest(res);
        })
        .WithTags("Auth")
        .AllowAnonymous();
    }
}
