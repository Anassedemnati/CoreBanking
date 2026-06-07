using CoreBanking.Clients.Application;

namespace CoreBanking.Clients.Infrastructure;

public sealed class UnitOfWork(ClientsWriteDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
