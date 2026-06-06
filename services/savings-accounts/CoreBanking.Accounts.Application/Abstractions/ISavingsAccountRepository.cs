using CoreBanking.Accounts.Domain;

namespace CoreBanking.Accounts.Application.Abstractions;

public interface ISavingsAccountRepository
{
    void Add(SavingsAccount account);
    Task<SavingsAccount?> FindAsync(Guid id, CancellationToken ct = default);
}
