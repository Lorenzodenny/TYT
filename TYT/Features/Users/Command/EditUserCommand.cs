using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;
using TYT.Dispatcher.Interface;
using TYT.Models;
using TYT.Services.Security;
using TYT.Shared;
using static TYT.Shared.Enums;

namespace TYT.Features.Users.Command;

// Command
public sealed record EditUserCommand(
    string Id,
    string Email,
    string? Nome,
    string? Cognome,
    TYTRole Role
) : ICommand<EditUserResponse>;

// Validator
public sealed class EditUserValidator : AbstractValidator<EditUserCommand>
{
    public EditUserValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Nome).MaximumLength(100).When(x => x.Nome != null);
        RuleFor(x => x.Cognome).MaximumLength(100).When(x => x.Cognome != null);
        RuleFor(x => x.Role).IsInEnum();
    }
}

// Handler
public sealed class EditUserHandler(
    UserManager<TYTUser> userManager,
    IRoleClaimsService roles)
    : ICommandHandler<EditUserCommand, EditUserResponse>
{
    public async Task<EditUserResponse> Handle(EditUserCommand req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.Id)
                   ?? throw new KeyNotFoundException("Utente non trovato.");

        if (user.IsDeleted) throw new InvalidOperationException("Utente eliminato.");

        // Aggiorna base
        user.Email = req.Email;
        user.UserName = req.Email; // standard: UserName = Email
        user.Nome = req.Nome;
        user.Cognome = req.Cognome;

        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
            throw new InvalidOperationException(string.Join("; ", update.Errors.Select(e => $"{e.Code}: {e.Description}")));

        // Aggiorna ruolo come claim 
        await roles.SetRoleAsync(user, req.Role, ct);

        return new EditUserResponse(user.Id, user.Email!, user.Nome, user.Cognome, req.Role.ToString());
    }
}

// Response DTO
public sealed record EditUserResponse(
    string Id,
    string Email,
    string? Nome,
    string? Cognome,
    string Role
);

// Endpoint
public sealed class EditUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/edit-user/{id}", async (
            [FromBody] EditUserCommand body,
            [FromServices] ISender sender,
            [FromServices] IValidator<EditUserCommand> validator,
            CancellationToken ct) =>
        {
            var v = await validator.ValidateAsync(body, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());

            try
            {
                var res = await sender.Send(body, ct);
                return Results.Ok(res);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 400); }
        })
        .WithTags("Users")
        .RequireAuthorization(PolicyNames.AdminOnly);
    }
}
