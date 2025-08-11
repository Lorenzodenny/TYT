using Carter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TYT.Dispatcher.Interface;
using TYT.Models;
using TYT.Shared;

namespace TYT.Features.Users.Command;

// Command
public sealed record SoftDeleteUserCommand(string Id) : ICommand<Unit>;

// Validator
public sealed class SoftDeleteUserValidator : AbstractValidator<SoftDeleteUserCommand>
{
    public SoftDeleteUserValidator() => RuleFor(x => x.Id).NotEmpty();
}

// Handler
public sealed class SoftDeleteUserHandler(UserManager<TYTUser> userManager)
    : ICommandHandler<SoftDeleteUserCommand, Unit>
{
    public async Task<Unit> Handle(SoftDeleteUserCommand req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.Id)
                   ?? throw new KeyNotFoundException("Utente non trovato.");

        if (!user.IsDeleted)
        {
            user.IsDeleted = true;
            var res = await userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                var msg = string.Join("; ", res.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new InvalidOperationException(msg);
            }
        }

        return Unit.Value;
    }
}

// Endpoint
public sealed class SoftDeleteUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/delete-users/{id}", async (
            [FromRoute] string id,
            [FromServices] ISender sender,
            [FromServices] IValidator<SoftDeleteUserCommand> validator,
            CancellationToken ct) =>
        {
            var cmd = new SoftDeleteUserCommand(id);

            var v = await validator.ValidateAsync(cmd, ct);
            if (!v.IsValid) return Results.ValidationProblem(v.ToDictionary());

            try
            {
                await sender.Send(cmd, ct);
                return Results.NoContent(); 
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { Error = ex.Message }); }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 400); }
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .WithTags("Users")
        .RequireAuthorization(PolicyNames.AdminOnly);
    }
}
