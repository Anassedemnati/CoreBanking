using CoreBanking.Accounts.Domain.Events;
using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Accounts.Domain;

/// <summary>
/// Link aggregate that ties two savings-account transaction legs (a withdrawal and
/// a deposit) together into a single account-to-account money transfer.
///
/// Holds only by-id references — no EF navigation properties — so each
/// <see cref="SavingsAccount"/> aggregate remains independently loadable.
/// </summary>
public sealed class AccountTransfer : AggregateRoot, IAuditable
{
    /// <summary>Id of the source (debited) savings account.</summary>
    public Guid SourceAccountId { get; private set; }

    /// <summary>Id of the destination (credited) savings account.</summary>
    public Guid DestinationAccountId { get; private set; }

    /// <summary>Id of the withdrawal transaction on the source account.</summary>
    public Guid SourceTransactionId { get; private set; }

    /// <summary>Id of the deposit transaction on the destination account.</summary>
    public Guid DestinationTransactionId { get; private set; }

    /// <summary>Transfer amount (always positive).</summary>
    public decimal Amount { get; private set; }

    /// <summary>ISO 4217 currency code shared by both accounts.</summary>
    public string CurrencyCode { get; private set; } = default!;

    /// <summary>Effective date of the transfer (applied to both legs).</summary>
    public DateOnly TransferDate { get; private set; }

    /// <summary>Caller-supplied description (max 100 characters).</summary>
    public string Description { get; private set; } = default!;

    /// <summary>Optional idempotency key supplied by the caller.</summary>
    public string? ClientTransferReference { get; private set; }

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private AccountTransfer(Guid id) : base(id) { }  // EF constructor

    /// <summary>
    /// Creates a new transfer link record after both legs have been booked.
    /// Raises <see cref="MoneyTransferred"/>.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when <paramref name="description"/> exceeds 100 characters
    /// (<c>account.transfer.description.toolong</c>).
    /// </exception>
    public static AccountTransfer Create(
        Guid sourceAccountId,
        Guid destinationAccountId,
        Guid sourceTransactionId,
        Guid destinationTransactionId,
        decimal amount,
        string currencyCode,
        DateOnly transferDate,
        string description,
        string? clientTransferReference)
    {
        if (description.Length > 100)
            throw new DomainException(
                "account.transfer.description.toolong",
                $"Transfer description must not exceed 100 characters (got {description.Length}).");

        var transfer = new AccountTransfer(Guid.CreateVersion7())
        {
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            SourceTransactionId = sourceTransactionId,
            DestinationTransactionId = destinationTransactionId,
            Amount = amount,
            CurrencyCode = currencyCode,
            TransferDate = transferDate,
            Description = description,
            ClientTransferReference = clientTransferReference,
        };

        transfer.Raise(new MoneyTransferred(
            TransferId: transfer.Id,
            SourceAccountId: sourceAccountId,
            DestinationAccountId: destinationAccountId,
            SourceTransactionId: sourceTransactionId,
            DestinationTransactionId: destinationTransactionId,
            Amount: amount,
            CurrencyCode: currencyCode,
            TransferDate: transferDate,
            ClientTransferReference: clientTransferReference));

        return transfer;
    }
}
