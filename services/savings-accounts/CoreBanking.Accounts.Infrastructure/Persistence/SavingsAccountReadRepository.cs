using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.Accounts;
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
                a.WithdrawnOn))
            .FirstOrDefaultAsync(ct);
    }
}
