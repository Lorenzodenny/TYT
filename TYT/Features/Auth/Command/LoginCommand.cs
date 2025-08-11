using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using TYT.Dispatcher.Interface;
using TYT.Models;
using TYT.Services.Auth;
using TYT.Services.Security;
using System.IdentityModel.Tokens.Jwt;


namespace TYT.Features.Auth.Command;

// Command (Request)
public sealed record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;

public sealed record LoginResponse(
    bool Success,
    string Message,
    string? AccessToken,
    DateTime? AccessTokenExpiresUtc,
    string? RefreshToken,
    DateTime? RefreshTokenExpiresUtc
);

// Validator
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

// Handler
public sealed class LoginHandler(
    UserManager<TYTUser> userManager,
    IJwtTokenService tokenService,
     IJwtClaimsFactory claimsFactory
) : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null)
            return new(false, "Email o password non validi", null, null, null, null);

        if (!user.EmailConfirmed)
            return new(false, "Email non confermata", null, null, null, null);

        if (!await userManager.CheckPasswordAsync(user, req.Password))
            return new(false, "Email o password non validi", null, null, null, null);

        // Claims 
        var claims = await claimsFactory.BuildAsync(user, includeJti: true, ct);

        var (access, accessExp) = await tokenService.CreateAccessTokenAsync(user, claims, ct);
        var (refresh, refreshExp) = await tokenService.CreateAndStoreRefreshTokenAsync(user.Id, ct);

        return new(true, "Login ok", access, accessExp, refresh, refreshExp);
    }
}

// Endpoint
public sealed class LoginEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            [FromBody] LoginCommand cmd,
            [FromServices] ISender sender,
            [FromServices] IValidator<LoginCommand> validator,
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