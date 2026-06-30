using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.Accounts;
using CoreBanking.Accounts.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class SavingsAccountReadRepository(SavingsAccountsReadDbContext db) : ISavingsAccountReadRepository
{
    public async Task<AccountTransferDto?> GetAccountTransferAsync(Guid transferId, CancellationToken ct = default)
    {
        return await db.Set<AccountTransfer>()
            .Where(t => t.Id == transferId)
            .Select(t => new AccountTransferDto(
                t.Id,
                t.SourceAccountId,
                t.DestinationAccountId,
                t.SourceTransactionId,
                t.DestinationTransactionId,
                t.Amount,
                t.CurrencyCode,
                t.TransferDate,
                t.Description,
                t.ClientTransferReference,
                t.CreatedOnUtc))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SavingsAccountDto?> FindDtoAsync(Guid id, CancellationToken ct = default)
    {
        return await db.SavingsAccounts
            .Where(a => a.Id == id)
            .Select(a => new SavingsAccountDto(
                a.Id,
                a.AccountNo,
                a.ClientId,
                a.ProductId,
                a.Status.ToString(),
                a.CurrencyCode,
                a.NominalAnnualRate,
                a.SubmittedOn,
                a.ApprovedOn,
                a.ActivatedOn,
                a.RejectedOn,
                a.WithdrawnOn,
                a.AccountBalance,
                a.InterestPostedTillDate,
                a.ClosedOn))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<SavingsAccountDto>> ListAsync(CancellationToken ct = default)
    {
        return await db.SavingsAccounts
            .OrderByDescending(a => a.SubmittedOn)
            .Select(a => new SavingsAccountDto(
                a.Id,
                a.AccountNo,
                a.ClientId,
                a.ProductId,
                a.Status.ToString(),
                a.CurrencyCode,
                a.NominalAnnualRate,
                a.SubmittedOn,
                a.ApprovedOn,
                a.ActivatedOn,
                a.RejectedOn,
                a.WithdrawnOn,
                a.AccountBalance,
                a.InterestPostedTillDate,
                a.ClosedOn))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SavingsTransactionDto>> FindTransactionsAsync(
        Guid accountId, CancellationToken ct = default)
    {
        // 1. Load the transaction rows for this account (ordered).
        var txns = await db.Set<SavingsAccountTransaction>()
            .Where(t => t.AccountId == accountId)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Sequence)
            .Select(t => new SavingsTransactionDto(
                t.Id, (int)t.Type, t.Type.ToString(),
                t.TransactionDate, t.Amount, t.RunningBalance))
            .ToListAsync(ct);

        if (txns.Count == 0)
            return txns;

        // 2. Collect all transaction ids so we can filter the transfer table.
        var txIds = txns.Select(t => t.Id).ToHashSet();

        // 3. Find any ACCOUNT_TRANSFERS rows whose source OR destination transaction
        //    is in this account's transaction list.  Uses the indexed
        //    SOURCETRANSACTIONID / DESTINATIONTRANSACTIONID columns.
        var transfers = await db.Set<AccountTransfer>()
            .Where(a => txIds.Contains(a.SourceTransactionId)
                     || txIds.Contains(a.DestinationTransactionId))
            .Select(a => new
            {
                a.Id,
                a.SourceTransactionId,
                a.DestinationTransactionId,
                a.SourceAccountId,
                a.DestinationAccountId
            })
            .ToListAsync(ct);

        if (transfers.Count == 0)
            return txns;

        // 4. Resolve counterparty account numbers (one cheap id→accountNo lookup).
        var counterpartyIds = transfers
            .SelectMany(t => new[] { t.SourceAccountId, t.DestinationAccountId })
            .ToHashSet();

        var accountNos = await db.SavingsAccounts
            .Where(a => counterpartyIds.Contains(a.Id))
            .Select(a => new { a.Id, a.AccountNo })
            .ToListAsync(ct);

        var accountNoMap = accountNos.ToDictionary(a => a.Id, a => a.AccountNo);

        // 5. Build TransferLegInfo list for the enricher.
        var legs = transfers.Select(t => new TransferLegInfo(
            TransferId: t.Id,
            SourceTransactionId: t.SourceTransactionId,
            DestinationTransactionId: t.DestinationTransactionId,
            SourceAccountId: t.SourceAccountId,
            DestinationAccountId: t.DestinationAccountId,
            SourceAccountNo: accountNoMap.GetValueOrDefault(t.SourceAccountId),
            DestinationAccountNo: accountNoMap.GetValueOrDefault(t.DestinationAccountId)))
            .ToList();

        // 6. Merge — pure in-memory, testable without infrastructure.
        return TransactionEnricher.Enrich(txns, legs);
    }
}
