using CoreBanking.Clients.Application.Clients;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Clients.Api.Controllers;

/// <summary>
/// Manages client registration and lifecycle.
/// </summary>
/// <remarks>
/// Clients are people and businesses that have applied (or may apply) to the institution
/// for savings accounts. A client must be in <c>Active</c> status before a savings account
/// application can be submitted on their behalf.
///
/// Lifecycle: <c>Pending</c> → <c>Active</c> (via activate) or <c>Closed</c>.
/// </remarks>
[ApiController]
[Route("api/v1/clients")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class ClientsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Register a new client.
    /// </summary>
    /// <remarks>
    /// Creates a client record in <c>Pending</c> status. The client must subsequently be
    /// activated before they can open a savings account.
    ///
    /// **Mandatory fields:** <c>firstName</c>, <c>lastName</c>, <c>mobileNo</c>.
    ///
    /// **Optional fields:** <c>externalId</c> — a reference code from an external system
    /// (e.g. national ID, CRM reference). Must be unique if supplied.
    ///
    /// Corresponds to Fineract <c>POST /v1/clients</c>.
    /// </remarks>
    /// <param name="cmd">Client registration payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created client ID.</returns>
    /// <response code="201">Client registered successfully. Location header points to the new resource.</response>
    /// <response code="400">Validation failed — one or more mandatory fields are missing or invalid.</response>
    /// <response code="409">A client with the supplied <c>externalId</c> already exists.</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterClientCommand cmd, CancellationToken ct)
    {
        var id = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>
    /// Activate a pending client.
    /// </summary>
    /// <remarks>
    /// Transitions the client from <c>Pending</c> to <c>Active</c> status. Only clients in
    /// <c>Pending</c> status can be activated; calling this on an already-active or closed
    /// client returns <c>422 Unprocessable Entity</c>.
    ///
    /// **Mandatory fields:** <c>activationDate</c> — must be on or after the client's
    /// submission date and not in the future.
    ///
    /// Corresponds to Fineract <c>POST /v1/clients/{clientId}?command=activate</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the client to activate.</param>
    /// <param name="body">Activation date payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Client activated successfully.</response>
    /// <response code="404">No client found with the given <c>id</c>.</response>
    /// <response code="422">
    /// Business rule violation — client is not in <c>Pending</c> status
    /// (error code: <c>client.activate.invalid</c>).
    /// </response>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateClientRequest body, CancellationToken ct)
    {
        await mediator.Send(new ActivateClientCommand(id, body.ActivationDate), ct);
        return NoContent();
    }

    /// <summary>
    /// Retrieve a client by ID.
    /// </summary>
    /// <remarks>
    /// Returns the full client record including current status, audit timestamps, and
    /// all stored contact details.
    ///
    /// Corresponds to Fineract <c>GET /v1/clients/{clientId}</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the client.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Client data transfer object.</returns>
    /// <response code="200">Client found and returned.</response>
    /// <response code="404">No client found with the given <c>id</c>.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetClientByIdQuery(id), ct);
        return Ok(dto);
    }
}

/// <summary>Request body for the activate-client operation.</summary>
/// <param name="ActivationDate">
/// Date on which the client is being activated. Must not be in the future and must be
/// on or after the client's submission date.
/// </param>
public sealed record ActivateClientRequest(DateOnly ActivationDate);
