using CoreBanking.Clients.Application.Clients;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Clients.Api;

public static class ClientsEndpoints
{
    public static void MapClientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/clients").WithTags("Clients");

        group.MapPost("", async (
            [FromBody] RegisterClientCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return Results.Created($"/api/v1/clients/{id}", new { id });
        })
        .Produces<object>(201)
        .ProducesValidationProblem()
        .WithSummary("Register a new client");

        group.MapPost("{id:guid}/activate", async (
            Guid id,
            [FromBody] ActivateClientRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new ActivateClientCommand(id, body.ActivationDate), ct);
            return Results.NoContent();
        })
        .Produces(204)
        .ProducesProblem(404)
        .WithSummary("Activate a client");

        group.MapGet("{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetClientByIdQuery(id), ct);
            return Results.Ok(dto);
        })
        .Produces<ClientDto>(200)
        .ProducesProblem(404)
        .WithSummary("Get client by ID");
    }
}

public sealed record ActivateClientRequest(DateOnly ActivationDate);
