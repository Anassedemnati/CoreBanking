using CoreBanking.Products.Application.Products;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Products.Api;

public static class SavingsProductsEndpoints
{
    public static void MapSavingsProductsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/savingsproducts").WithTags("SavingsProducts");

        group.MapPost("", async (
            [FromBody] CreateSavingsProductCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return Results.Created($"/api/v1/savingsproducts/{id}", new { id });
        })
        .Produces<object>(201)
        .ProducesValidationProblem()
        .WithSummary("Create a new savings product");

        group.MapGet("{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetSavingsProductByIdQuery(id), ct);
            return Results.Ok(dto);
        })
        .Produces<SavingsProductDto>(200)
        .ProducesProblem(404)
        .WithSummary("Get savings product by ID");

        group.MapGet("", async (
            IMediator mediator,
            CancellationToken ct) =>
        {
            var dtos = await mediator.Send(new ListSavingsProductsQuery(), ct);
            return Results.Ok(dtos);
        })
        .Produces<IReadOnlyList<SavingsProductDto>>(200)
        .WithSummary("List all savings products");
    }
}
