using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.Accounts;
using CoreBanking.Accounts.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class SavingsAccountReadRepository(SavingsAccountsReadDbContext db) : ISavingsAccountReadRepository
{
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
        return await db.Set<SavingsAccountTransaction>()
            .Where(t => t.AccountId == accountId)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Sequence)
            .Select(t => new SavingsTransactionDto(
                t.Id, (int)t.Type, t.Type.ToString(),
                t.TransactionDate, t.Amount, t.RunningBalance))
            .ToListAsync(ct);
    }
}
