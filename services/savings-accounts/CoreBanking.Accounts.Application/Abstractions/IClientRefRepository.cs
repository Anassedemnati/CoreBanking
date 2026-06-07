using CoreBanking.Accounts.Application.ReadModels;

namespace CoreBanking.Accounts.Application.Abstractions;

public interface IClientRefRepository
{
    Task<ClientRef?> FindAsync(Guid clientId, CancellationToken ct = default);
    Task UpsertAsync(ClientRef clientRef, CancellationToken ct = default);
}
