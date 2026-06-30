using CoreBanking.Accounts.Domain;

namespace CoreBanking.Accounts.Application.Abstractions;

public interface IAccountTransferRepository
{
    void Add(AccountTransfer transfer);
    Task<AccountTransfer?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<AccountTransfer?> FindByClientReferenceAsync(string clientTransferReference, CancellationToken ct = default);
}
