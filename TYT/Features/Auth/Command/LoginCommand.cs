using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TYT.Dispatcher.Interface;
using TYT.Models;

namespace TYT.Features.Auth.Command;

// Command (Request)
public sealed record LoginCommand(string Email, string Password, bool RememberMe) : ICommand<LoginResponse>;

// Validator
public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

// Response
public sealed record LoginResponse(bool Success, string? Message);

// Handler
public sealed class LoginHandler(
    SignInManager<TYTUser> signInManager,
    UserManager<TYTUser> userManager
) : ICommandHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);
        if (user is null || user.IsDeleted)
            return new(false, "Credenziali non valide.");

        var result = await signInManager.PasswordSignInAsync(
            user, req.Password, req.RememberMe, lockoutOnFailure: true);

        if (!result.Succeeded)
            return new(false, result.IsLockedOut ? "Account bloccato temporaneamente." : "Credenziali non valide.");

        // Cookie creato qui (con tutti i claim dell'utente, incluso "TYTRole").
        return new(true, "Login effettuato.");
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
