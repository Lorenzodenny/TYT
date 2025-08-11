using Carter;
using FluentValidation;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using TYT.Data;
using TYT.Dispatcher.Interface;
using TYT.Models;
using TYT.Services.Security;
using TYT.Shared;
using static TYT.Shared.Enums;

namespace TYT.Features.Users.Command;

// Command
public sealed record CreateUserCommand(
    string Email,
    string Password,
    string? Nome,
    string? Cognome,
    TYTRole Role
) : ICommand<CreateUserResponse>;

// Validator
public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Nome).MaximumLength(100).When(x => x.Nome != null);
        RuleFor(x => x.Cognome).MaximumLength(100).When(x => x.Cognome != null);
        RuleFor(x => x.Role).IsInEnum();
    }
}

// Handler
public sealed class CreateUserHandler(
    UserManager<TYTUser> userManager,
     IRoleClaimsService roles,
     IHttpContextAccessor accessor,
     IBackgroundJobClient bg,
     TYT.Services.EmailService.EmailSenderHelper email
) : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    public async Task<CreateUserResponse> Handle(CreateUserCommand req, CancellationToken ct)
    {
        var user = new TYTUser
        {
            Email = req.Email,
            UserName = req.Email,   
            Nome = req.Nome,
            Cognome = req.Cognome
        };

        var create = await userManager.CreateAsync(user, req.Password);
        if (!create.Succeeded)
        {
            var msg = string.Join("; ", create.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException(msg);
        }

        // Ruolo via claim
        await roles.SetRoleAsync(user, req.Role, ct);

        // genera token e invia email di conferma
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        // Recupero baseUrl ora, mentre HttpContext esiste
        var reqHttp = accessor.HttpContext?.Request
            ?? throw new InvalidOperationException("HttpContext non disponibile.");
        var baseUrl = $"{reqHttp.Scheme}://{reqHttp.Host}";

        // Metto il job in coda (senza dipendere da HttpContext)
        bg.Enqueue(() => email.SendEmailConfirmationAsync(
            user.Id,
            user.Email!,
            user.Nome,
            token,
            baseUrl
        ));

        return new CreateUserResponse(user.Id, user.Email!, user.Nome, user.Cognome, req.Role.ToString());
    }
}

// Response DTO
public sealed record CreateUserResponse(
    string Id,
    string Email,
    string? Nome,
    string? Cognome,
    string Role
);

// Endpoint
public sealed class CreateUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/create-users", async (
            [FromServices] ISender sender,
            [FromBody] CreateUserCommand command,
            [FromServices] IValidator<CreateUserCommand> validator,
            CancellationToken ct) =>
        {
            var v = await validator.ValidateAsync(command, ct);
            if (!v.IsValid)
                return Results.ValidationProblem(v.ToDictionary());

            try
            {
                var res = await sender.Send(command, ct);
                return Results.Created($"/api/users/{res.Id}", res);
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message, statusCode: 400);
            }
        })
        .WithTags("Users");
        //.RequireAuthorization(PolicyNames.SuperAdminOnly);
    }
}
