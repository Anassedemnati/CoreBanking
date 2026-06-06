using CoreBanking.Clients.Application.Clients;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Clients.Api.Controllers;

[ApiController]
[Route("api/v1/clients")]
public sealed class ClientsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterClientCommand cmd, CancellationToken ct)
    {
        var id = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateClientRequest body, CancellationToken ct)
    {
        await mediator.Send(new ActivateClientCommand(id, body.ActivationDate), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetClientByIdQuery(id), ct);
        return Ok(dto);
    }
}

public sealed record ActivateClientRequest(DateOnly ActivationDate);
