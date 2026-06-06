using CoreBanking.Accounts.Application.Accounts;

namespace CoreBanking.Accounts.Application.Abstractions;

public interface ISavingsAccountReadRepository
{
    Task<SavingsAccountDto?> FindDtoAsync(Guid id, CancellationToken ct = default);
}
