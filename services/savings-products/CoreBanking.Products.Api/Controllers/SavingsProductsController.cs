using CoreBanking.Products.Application.Products;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Products.Api.Controllers;

[ApiController]
[Route("api/v1/savingsproducts")]
public sealed class SavingsProductsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSavingsProductCommand cmd, CancellationToken ct)
    {
        var id = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SavingsProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetSavingsProductByIdQuery(id), ct);
        return Ok(dto);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SavingsProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var dtos = await mediator.Send(new ListSavingsProductsQuery(), ct);
        return Ok(dtos);
    }
}
