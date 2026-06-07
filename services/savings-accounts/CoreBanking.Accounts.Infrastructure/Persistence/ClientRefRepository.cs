using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.ReadModels;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class ClientRefRepository(SavingsAccountsWriteDbContext db) : IClientRefRepository
{
    public Task<ClientRef?> FindAsync(Guid clientId, CancellationToken ct = default)
        => db.Set<ClientRef>().FindAsync([clientId], ct).AsTask();

    public async Task UpsertAsync(ClientRef clientRef, CancellationToken ct = default)
    {
        var existing = await db.Set<ClientRef>().FindAsync([clientRef.ClientId], ct);
        if (existing is null)
        {
            db.Set<ClientRef>().Add(clientRef);
        }
        else
        {
            existing.DisplayName = clientRef.DisplayName;
            existing.IsActive = clientRef.IsActive;
            existing.EventVersion = clientRef.EventVersion;
        }
    }
}
