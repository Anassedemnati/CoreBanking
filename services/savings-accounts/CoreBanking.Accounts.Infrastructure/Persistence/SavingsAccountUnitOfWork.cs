using CoreBanking.Accounts.Application.Abstractions;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class SavingsAccountUnitOfWork(SavingsAccountsWriteDbContext db) : ISavingsAccountUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
