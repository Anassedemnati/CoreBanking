using CoreBanking.Accounts.Application.Accounts;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Accounts.Api.Controllers;

[ApiController]
[Route("api/v1/savingsaccounts")]
public sealed class SavingsAccountsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit([FromBody] SubmitSavingsApplicationCommand cmd, CancellationToken ct)
    {
        var id = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new ApproveSavingsAccountCommand(id, body.ApprovedOn), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new ActivateSavingsAccountCommand(id, body.ActivatedOn), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new RejectSavingsAccountCommand(id, body.RejectedOn), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new WithdrawSavingsApplicationCommand(id, body.WithdrawnOn), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SavingsAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetSavingsAccountByIdQuery(id), ct);
        return Ok(dto);
    }
}

public sealed record ApproveAccountRequest(DateOnly ApprovedOn);
public sealed record ActivateAccountRequest(DateOnly ActivatedOn);
public sealed record RejectAccountRequest(DateOnly RejectedOn);
public sealed record WithdrawAccountRequest(DateOnly WithdrawnOn);
