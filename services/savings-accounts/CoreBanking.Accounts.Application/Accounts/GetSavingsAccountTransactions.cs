using CoreBanking.Accounts.Application.Abstractions;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

/// <summary>
/// Carries transfer metadata attached to a transaction that is part of an
/// account-to-account transfer.  Null on any manual deposit / withdrawal /
/// interest-posting transaction.
/// </summary>
public sealed record TransferInfo(
    Guid TransferId,
    string Direction,          // "out" (source/withdrawal leg) or "in" (destination/deposit leg)
    Guid CounterpartyAccountId,
    string? CounterpartyAccountNo);

public sealed record SavingsTransactionDto(
    Guid Id,
    int TypeId,
    string Type,
    DateOnly TransactionDate,
    decimal Amount,
    decimal RunningBalance,
    TransferInfo? Transfer = null);

/// <summary>
/// Input row passed from the read repository to <see cref="TransactionEnricher"/>,
/// representing one side of an <c>ACCOUNT_TRANSFERS</c> record.
/// </summary>
public sealed record TransferLegInfo(
    Guid TransferId,
    Guid SourceTransactionId,
    Guid DestinationTransactionId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    string? SourceAccountNo,
    string? DestinationAccountNo);

/// <summary>
/// Pure in-memory helper that merges a flat list of <see cref="TransferLegInfo"/>
/// rows (queried from <c>ACCOUNT_TRANSFERS</c>) into the transaction DTOs.
/// Extracted so it can be unit-tested without a database.
/// </summary>
public static class TransactionEnricher
{
    /// <param name="transactions">Already-ordered list from the read repo (no transfer block yet).</param>
    /// <param name="legs">Transfer records whose SourceTransactionId or DestinationTransactionId
    /// overlaps with any id in <paramref name="transactions"/>.</param>
    /// <returns>New list with <see cref="SavingsTransactionDto.Transfer"/> populated where applicable.</returns>
    public static IReadOnlyList<SavingsTransactionDto> Enrich(
        IReadOnlyList<SavingsTransactionDto> transactions,
        IReadOnlyList<TransferLegInfo> legs)
    {
        if (legs.Count == 0)
            return transactions;

        // Build a lookup: txId → TransferInfo to attach
        var lookup = new Dictionary<Guid, TransferInfo>(legs.Count * 2);

        foreach (var leg in legs)
        {
            // Source transaction = withdrawal → direction "out"; counterparty = destination
            lookup[leg.SourceTransactionId] = new TransferInfo(
                leg.TransferId,
                "out",
                leg.DestinationAccountId,
                leg.DestinationAccountNo);

            // Destination transaction = deposit → direction "in"; counterparty = source
            lookup[leg.DestinationTransactionId] = new TransferInfo(
                leg.TransferId,
                "in",
                leg.SourceAccountId,
                leg.SourceAccountNo);
        }

        var result = new List<SavingsTransactionDto>(transactions.Count);
        foreach (var tx in transactions)
        {
            result.Add(lookup.TryGetValue(tx.Id, out var info)
                ? tx with { Transfer = info }
                : tx);
        }

        return result;
    }
}

public sealed record GetSavingsAccountTransactionsQuery(Guid AccountId)
    : IQuery<IReadOnlyList<SavingsTransactionDto>>;

public sealed class GetSavingsAccountTransactionsHandler(ISavingsAccountReadRepository readRepo)
    : IQueryHandler<GetSavingsAccountTransactionsQuery, IReadOnlyList<SavingsTransactionDto>>
{
    public async ValueTask<IReadOnlyList<SavingsTransactionDto>> Handle(
        GetSavingsAccountTransactionsQuery query, CancellationToken ct)
    {
        return await readRepo.FindTransactionsAsync(query.AccountId, ct);
    }
}
