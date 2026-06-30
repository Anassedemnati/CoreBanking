using CoreBanking.Accounts.Application.Accounts;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Accounts.Api.Controllers;

/// <summary>
/// Manages account-to-account money transfers within the savings domain.
/// </summary>
/// <remarks>
/// A transfer books a withdrawal on the source account and a deposit on the destination account
/// atomically in a single database transaction. Both accounts must be Active (300) and share the
/// same currency.
///
/// **Idempotency:** the optional <c>clientTransferReference</c> field acts as an idempotency key.
/// A repeat request with the same reference and identical payload returns the existing transfer id
/// (200 not 201). A conflicting payload raises 422 (<c>account.transfer.idempotency.conflict</c>).
///
/// **Error codes (422):** <c>account.transfer.currency.mismatch</c>,
/// <c>account.transfer.source.notactive</c>, <c>account.transfer.destination.notactive</c>,
/// <c>account.transfer.source.beforepivot</c>, <c>account.transfer.destination.beforepivot</c>,
/// <c>account.transfer.source.beforeactivation</c>, <c>account.transfer.destination.beforeactivation</c>,
/// <c>account.transfer.idempotency.conflict</c>, <c>account.transfer.amount.precision</c>.
///
/// **Error codes (400):** validator failures including <c>account.transfer.from.to.same.account</c>.
/// </remarks>
[ApiController]
[Route("api/v1/accounttransfers")]
[Produces("application/json")]
[Consumes("application/json")]
[Authorize]
public sealed class AccountTransfersController(IMediator mediator) : ControllerBase
{
    /// <summary>Initiate an account-to-account money transfer.</summary>
    /// <remarks>
    /// Atomically withdraws <c>amount</c> from the source account and deposits it into the
    /// destination account on <c>transferDate</c>. Both accounts must be Active and share the
    /// same currency. The transfer date must be strictly after each account's interest posting
    /// pivot date (forward-only immutability applies to both legs).
    ///
    /// Returns <c>201 Created</c> with the new transfer id and a <c>Location</c> header.
    /// If <c>clientTransferReference</c> is supplied and a matching transfer already exists with
    /// identical source/destination/amount, the existing id is returned instead (idempotent replay).
    /// </remarks>
    /// <param name="request">Transfer request payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="201">Transfer completed. Location header points to the new resource.</response>
    /// <response code="400">Validation failed — e.g. missing fields, self-transfer, non-positive amount.</response>
    /// <response code="403">Caller is not authenticated or authorized.</response>
    /// <response code="404">Source or destination account not found.</response>
    /// <response code="409">Optimistic concurrency conflict — one of the accounts changed mid-transfer; retry.</response>
    /// <response code="422">
    /// Business rule violation — currency mismatch, account not active, before pivot/activation date,
    /// insufficient funds, idempotency conflict, or amount precision error.
    /// </response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request, CancellationToken ct)
    {
        var id = await mediator.Send(
            new TransferBetweenSavingsAccountsCommand(
                request.SourceAccountId,
                request.DestinationAccountId,
                request.TransferDate,
                request.Amount,
                request.Description,
                request.ClientTransferReference),
            ct);

        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>Retrieve a transfer record by ID.</summary>
    /// <remarks>
    /// Returns the transfer details including both account IDs, both transaction IDs,
    /// amount, currency, transfer date, description, and optional idempotency key.
    /// </remarks>
    /// <param name="id">Unique identifier of the transfer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Transfer found and returned.</response>
    /// <response code="404">No transfer found with the given <c>id</c>.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountTransferDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetAccountTransferByIdQuery(id), ct);
        return Ok(dto);
    }
}

/// <summary>Request body for the account-to-account transfer operation.</summary>
/// <param name="SourceAccountId">ID of the account to debit.</param>
/// <param name="DestinationAccountId">ID of the account to credit.</param>
/// <param name="TransferDate">Value date for both legs (ISO-8601; not in the future).</param>
/// <param name="Amount">Strictly positive amount in the shared currency.</param>
/// <param name="Description">Transfer description (1–100 characters).</param>
/// <param name="ClientTransferReference">Optional caller-supplied idempotency key.</param>
public sealed record TransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    DateOnly TransferDate,
    decimal Amount,
    string Description,
    string? ClientTransferReference = null);
