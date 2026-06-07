using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class SavingsAccountRepository(SavingsAccountsWriteDbContext db) : ISavingsAccountRepository
{
    public void Add(SavingsAccount account) => db.SavingsAccounts.Add(account);

    public async Task<SavingsAccount?> FindAsync(Guid id, CancellationToken ct = default)
        => await db.SavingsAccounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
}
