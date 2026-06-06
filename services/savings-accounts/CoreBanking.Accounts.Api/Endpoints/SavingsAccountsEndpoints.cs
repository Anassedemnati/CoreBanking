using CoreBanking.Accounts.Application.Accounts;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Accounts.Api;

public static class SavingsAccountsEndpoints
{
    public static void MapSavingsAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/savingsaccounts").WithTags("SavingsAccounts");

        group.MapPost("", async (
            [FromBody] SubmitSavingsApplicationCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return Results.Created($"/api/v1/savingsaccounts/{id}", new { id });
        })
        .Produces<object>(201)
        .ProducesValidationProblem()
        .WithSummary("Submit a savings account application");

        group.MapPost("{id:guid}/approve", async (
            Guid id,
            [FromBody] ApproveAccountRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new ApproveSavingsAccountCommand(id, body.ApprovedOn), ct);
            return Results.NoContent();
        })
        .Produces(204)
        .ProducesProblem(404)
        .WithSummary("Approve a savings account");

        group.MapPost("{id:guid}/activate", async (
            Guid id,
            [FromBody] ActivateAccountRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new ActivateSavingsAccountCommand(id, body.ActivatedOn), ct);
            return Results.NoContent();
        })
        .Produces(204)
        .ProducesProblem(404)
        .WithSummary("Activate a savings account");

        group.MapPost("{id:guid}/reject", async (
            Guid id,
            [FromBody] RejectAccountRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new RejectSavingsAccountCommand(id, body.RejectedOn), ct);
            return Results.NoContent();
        })
        .Produces(204)
        .ProducesProblem(404)
        .WithSummary("Reject a savings account");

        group.MapPost("{id:guid}/withdraw", async (
            Guid id,
            [FromBody] WithdrawAccountRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new WithdrawSavingsApplicationCommand(id, body.WithdrawnOn), ct);
            return Results.NoContent();
        })
        .Produces(204)
        .ProducesProblem(404)
        .WithSummary("Withdraw a savings account application");

        group.MapGet("{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetSavingsAccountByIdQuery(id), ct);
            return Results.Ok(dto);
        })
        .Produces<SavingsAccountDto>(200)
        .ProducesProblem(404)
        .WithSummary("Get savings account by ID");
    }
}

public sealed record ApproveAccountRequest(DateOnly ApprovedOn);
public sealed record ActivateAccountRequest(DateOnly ActivatedOn);
public sealed record RejectAccountRequest(DateOnly RejectedOn);
public sealed record WithdrawAccountRequest(DateOnly WithdrawnOn);
