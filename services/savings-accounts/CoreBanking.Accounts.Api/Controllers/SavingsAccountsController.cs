using CoreBanking.Accounts.Application.Accounts;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace CoreBanking.Accounts.Api.Controllers;

/// <summary>
/// Manages the savings account lifecycle.
/// </summary>
/// <remarks>
/// A savings account passes through a defined state machine derived from Apache Fineract.
/// Status codes follow the Fineract convention:
///
/// | Code | Status    | Description                                              |
/// |------|-----------|----------------------------------------------------------|
/// | 100  | Submitted | Application received, awaiting approval.                 |
/// | 200  | Approved  | Application approved, awaiting first activation.         |
/// | 300  | Active    | Account is fully operational.                            |
/// | 400  | Withdrawn | Applicant withdrew the application before approval.      |
/// | 500  | Rejected  | Institution rejected the application.                    |
/// | 600  | Closed    | Account has been closed (future use).                    |
///
/// **Valid transitions:**
/// - Submit → Submitted (100)
/// - Submitted → Approved (200) via <c>approve</c>
/// - Approved  → Active (300) via <c>activate</c>
/// - Submitted → Rejected (500) via <c>reject</c>
/// - Submitted → Withdrawn (400) via <c>withdraw</c>
///
/// All other transitions return <c>422 Unprocessable Entity</c>.
///
/// Once <c>Active</c>, the account accepts deposit/withdrawal transactions and
/// interest posting (see the transaction endpoints below). Money can never make
/// the balance negative; backdated entries are allowed back to the interest pivot date.
///
/// The client and product referenced in the submit request are validated against local
/// read models (CLIENT_REF, PRODUCT_REF) that are kept up-to-date via Kafka consumers
/// subscribed to the <c>clients.events</c> and <c>products.events</c> compacted topics.
/// </remarks>
[ApiController]
[Route("api/v1/savingsaccounts")]
[Produces("application/json")]
[Consumes("application/json")]
public sealed class SavingsAccountsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Submit a new savings account application.
    /// </summary>
    /// <remarks>
    /// Creates a savings account in <c>Submitted</c> (100) status. The account inherits its
    /// currency and default interest rate from the referenced savings product at submission time.
    ///
    /// **Mandatory fields:**
    /// - <c>clientId</c> — must reference an existing client in <c>Active</c> status.
    /// - <c>productId</c> — must reference an existing savings product.
    /// - <c>accountNo</c> — unique account number (max 50 characters).
    /// - <c>currencyCode</c> — ISO 4217 three-letter code, must match the product's currency.
    /// - <c>currencyDecimalPlaces</c> — must match the product's currency decimal places.
    /// - <c>nominalAnnualRate</c> — annual interest rate in percent (≥ 0).
    /// - <c>submittedOn</c> — submission date, must not be in the future.
    ///
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts</c>.
    /// </remarks>
    /// <param name="cmd">Savings account application payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created savings account ID.</returns>
    /// <response code="201">Application submitted. Location header points to the new resource.</response>
    /// <response code="400">Validation failed — mandatory fields missing or values invalid.</response>
    /// <response code="422">
    /// Business rule violation, e.g. client not active (<c>account.client.inactive</c>)
    /// or client/product not found (<c>account.client.notfound</c>, <c>account.product.notfound</c>).
    /// </response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Submit([FromBody] SubmitSavingsApplicationCommand cmd, CancellationToken ct)
    {
        var id = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    /// <summary>
    /// Approve a submitted savings account application.
    /// </summary>
    /// <remarks>
    /// Transitions the account from <c>Submitted</c> (100) to <c>Approved</c> (200) status.
    /// Only accounts currently in <c>Submitted</c> status can be approved.
    ///
    /// **Mandatory fields:** <c>approvedOn</c> — must be on or after <c>submittedOn</c>.
    ///
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}?command=approve</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings account.</param>
    /// <param name="body">Approval date payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Account approved successfully.</response>
    /// <response code="404">No savings account found with the given <c>id</c>.</response>
    /// <response code="422">
    /// Account is not in <c>Submitted</c> status (error code: <c>account.approve.invalid</c>).
    /// </response>
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new ApproveSavingsAccountCommand(id, body.ApprovedOn), ct);
        return NoContent();
    }

    /// <summary>
    /// Activate an approved savings account.
    /// </summary>
    /// <remarks>
    /// Transitions the account from <c>Approved</c> (200) to <c>Active</c> (300) status.
    /// Only accounts in <c>Approved</c> status can be activated. Once active, the account
    /// is fully operational and can receive deposits.
    ///
    /// **Mandatory fields:** <c>activatedOn</c> — must be on or after <c>approvedOn</c>.
    ///
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}?command=activateSavings</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings account.</param>
    /// <param name="body">Activation date payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Account activated successfully.</response>
    /// <response code="404">No savings account found with the given <c>id</c>.</response>
    /// <response code="422">
    /// Account is not in <c>Approved</c> status (error code: <c>account.activate.invalid</c>).
    /// </response>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate(Guid id, [FromBody] ActivateAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new ActivateSavingsAccountCommand(id, body.ActivatedOn), ct);
        return NoContent();
    }

    /// <summary>
    /// Reject a submitted savings account application.
    /// </summary>
    /// <remarks>
    /// Transitions the account from <c>Submitted</c> (100) to <c>Rejected</c> (500) status.
    /// This is a terminal state — a rejected account cannot be re-submitted. A new application
    /// must be created instead.
    ///
    /// Only accounts in <c>Submitted</c> status can be rejected.
    ///
    /// **Mandatory fields:** <c>rejectedOn</c> — must be on or after <c>submittedOn</c>.
    ///
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}?command=reject</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings account.</param>
    /// <param name="body">Rejection date payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Application rejected successfully.</response>
    /// <response code="404">No savings account found with the given <c>id</c>.</response>
    /// <response code="422">
    /// Account is not in <c>Submitted</c> status (error code: <c>account.reject.invalid</c>).
    /// </response>
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new RejectSavingsAccountCommand(id, body.RejectedOn), ct);
        return NoContent();
    }

    /// <summary>
    /// Withdraw a savings account application.
    /// </summary>
    /// <remarks>
    /// Transitions the account from <c>Submitted</c> (100) to <c>Withdrawn</c> (400) status.
    /// The applicant is withdrawing their own application before a decision is made.
    /// This is a terminal state — a withdrawn application cannot be reactivated.
    ///
    /// Only accounts in <c>Submitted</c> status can be withdrawn.
    ///
    /// **Mandatory fields:** <c>withdrawnOn</c> — must be on or after <c>submittedOn</c>.
    ///
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}?command=withdrawnByApplicant</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings account.</param>
    /// <param name="body">Withdrawal date payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Application withdrawn successfully.</response>
    /// <response code="404">No savings account found with the given <c>id</c>.</response>
    /// <response code="422">
    /// Account is not in <c>Submitted</c> status (error code: <c>account.withdraw.invalid</c>).
    /// </response>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Withdraw(Guid id, [FromBody] WithdrawAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new WithdrawSavingsApplicationCommand(id, body.WithdrawnOn), ct);
        return NoContent();
    }

    /// <summary>Deposit money into an active savings account.</summary>
    /// <remarks>
    /// Adds a credit transaction and updates the account balance. Backdated deposits are
    /// allowed back to (but not on or before) the interest posting pivot date.
    ///
    /// **Mandatory fields:**
    /// - <c>transactionDate</c> — value date, not in the future, not before activation.
    /// - <c>amount</c> — strictly positive, in the account currency.
    ///
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}/transactions?command=deposit</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings account.</param>
    /// <param name="body">Transaction payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Deposit recorded; returns the transaction id.</response>
    /// <response code="404">No savings account found with the given <c>id</c>.</response>
    /// <response code="422">
    /// Business rule violation: <c>account.transaction.notactive</c>, <c>account.transaction.future</c>,
    /// <c>account.transaction.beforeactivation</c>, <c>account.transaction.beforepivot</c>,
    /// <c>account.transaction.amount.invalid</c>.
    /// </response>
    [HttpPost("{id:guid}/transactions/deposit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deposit(Guid id, [FromBody] TransactionRequest body, CancellationToken ct)
    {
        var txId = await mediator.Send(
            new DepositToSavingsAccountCommand(id, body.TransactionDate, body.Amount), ct);
        return Ok(new { transactionId = txId });
    }

    /// <summary>Withdraw money from an active savings account.</summary>
    /// <remarks>
    /// Adds a debit transaction. The running balance may never go negative at any point in
    /// the transaction timeline — including for backdated withdrawals — otherwise the
    /// operation is rejected (error code <c>account.balance.insufficient</c>).
    ///
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}/transactions?command=withdrawal</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings account.</param>
    /// <param name="body">Transaction payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Withdrawal recorded; returns the transaction id.</response>
    /// <response code="404">No savings account found with the given <c>id</c>.</response>
    /// <response code="422">Insufficient balance or transaction-date rule violation.</response>
    [HttpPost("{id:guid}/transactions/withdraw")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> WithdrawMoney(Guid id, [FromBody] TransactionRequest body, CancellationToken ct)
    {
        var txId = await mediator.Send(
            new WithdrawFromSavingsAccountCommand(id, body.TransactionDate, body.Amount), ct);
        return Ok(new { transactionId = txId });
    }

    /// <summary>Calculate and post interest for all completed posting periods up to a date.</summary>
    /// <remarks>
    /// Posts one interest transaction per completed posting period (dated the period end) and
    /// advances the interest pivot date. Partial trailing periods accrue and are posted on a
    /// later run. Idempotent: re-running with the same date posts nothing new.
    ///
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}?command=postInterest</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings account.</param>
    /// <param name="body">Posting date payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="204">Interest posted (or no completed period was pending).</response>
    /// <response code="404">No savings account found with the given <c>id</c>.</response>
    /// <response code="422">
    /// Account is not active (<c>account.postinterest.notactive</c>) or the posting date
    /// is in the future (<c>account.postinterest.future</c>).
    /// </response>
    [HttpPost("{id:guid}/postinterest")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PostInterest(Guid id, [FromBody] PostInterestRequest body, CancellationToken ct)
    {
        await mediator.Send(new PostInterestToSavingsAccountCommand(id, body.AsOf), ct);
        return NoContent();
    }

    /// <summary>Close an active savings account.</summary>
    /// <remarks>
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}?command=close</c>.
    /// Validates the close date, optionally sweeps the remaining balance to zero (dated the close
    /// date, pivot-exempt), then transitions the account to the terminal <c>Closed</c> (600) state.
    /// </remarks>
    /// <response code="204">Account closed.</response>
    /// <response code="404">Account not found.</response>
    /// <response code="422">
    /// Business rule violation: <c>account.close.notactive</c>, <c>account.close.beforeactivation</c>,
    /// <c>account.close.future</c>, <c>account.close.afterlasttransaction</c>,
    /// <c>account.close.balance.nonzero</c>.
    /// </response>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new CloseSavingsAccountCommand(id, body.ClosedOn, body.WithdrawBalance), ct);
        return NoContent();
    }

    /// <summary>List the account's transactions in chronological order.</summary>
    /// <remarks>
    /// Returns deposits, withdrawals and interest postings ordered by transaction date,
    /// each with its running balance at that point in the timeline.
    ///
    /// Corresponds to Fineract <c>GET /v1/savingsaccounts/{accountId}/transactions</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <response code="200">Transactions returned (empty list when none exist).</response>
    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<SavingsTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(Guid id, CancellationToken ct)
    {
        var txs = await mediator.Send(new GetSavingsAccountTransactionsQuery(id), ct);
        return Ok(txs);
    }

    /// <summary>
    /// Retrieve a savings account by ID.
    /// </summary>
    /// <remarks>
    /// Returns the full savings account record including current status code, linked client
    /// and product identifiers, currency settings, interest rate, all lifecycle dates
    /// (submitted, approved, activated, rejected, withdrawn), and audit timestamps.
    ///
    /// Corresponds to Fineract <c>GET /v1/savingsaccounts/{accountId}</c>.
    /// </remarks>
    /// <param name="id">Unique identifier of the savings account.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Savings account data transfer object.</returns>
    /// <response code="200">Savings account found and returned.</response>
    /// <response code="404">No savings account found with the given <c>id</c>.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SavingsAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var dto = await mediator.Send(new GetSavingsAccountByIdQuery(id), ct);
        return Ok(dto);
    }
}

/// <summary>Request body for the approve-account operation.</summary>
/// <param name="ApprovedOn">
/// Date on which the account application is being approved.
/// Must be on or after the account's <c>submittedOn</c> date.
/// </param>
public sealed record ApproveAccountRequest(DateOnly ApprovedOn);

/// <summary>Request body for the activate-account operation.</summary>
/// <param name="ActivatedOn">
/// Date on which the account is being activated.
/// Must be on or after the account's <c>approvedOn</c> date.
/// </param>
public sealed record ActivateAccountRequest(DateOnly ActivatedOn);

/// <summary>Request body for the reject-account operation.</summary>
/// <param name="RejectedOn">
/// Date on which the account application is being rejected.
/// Must be on or after the account's <c>submittedOn</c> date.
/// </param>
public sealed record RejectAccountRequest(DateOnly RejectedOn);

/// <summary>Request body for the withdraw-account operation.</summary>
/// <param name="WithdrawnOn">
/// Date on which the applicant is withdrawing their application.
/// Must be on or after the account's <c>submittedOn</c> date.
/// </param>
public sealed record WithdrawAccountRequest(DateOnly WithdrawnOn);

/// <summary>Request body for deposit and withdrawal operations.</summary>
/// <param name="TransactionDate">Value date of the transaction (not in the future).</param>
/// <param name="Amount">Strictly positive amount in the account currency.</param>
public sealed record TransactionRequest(DateOnly TransactionDate, decimal Amount);

/// <summary>Request body for the post-interest operation.</summary>
/// <param name="AsOf">Post interest for all posting periods ending on or before this date.</param>
public sealed record PostInterestRequest(DateOnly AsOf);

/// <summary>Request body for the close-account operation.</summary>
/// <param name="ClosedOn">Close date — not in the future, not before activation or the last transaction.</param>
/// <param name="WithdrawBalance">When true, sweep any remaining balance to zero (dated <c>ClosedOn</c>) before closing.</param>
public sealed record CloseAccountRequest(DateOnly ClosedOn, bool WithdrawBalance = false);
