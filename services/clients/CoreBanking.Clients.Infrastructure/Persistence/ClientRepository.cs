using CoreBanking.Clients.Application;
using CoreBanking.Clients.Domain;

namespace CoreBanking.Clients.Infrastructure;

public sealed class ClientRepository(ClientsWriteDbContext db) : IClientRepository
{
    public void Add(Client client) => db.Clients.Add(client);

    public async Task<Client?> FindAsync(Guid id, CancellationToken ct = default)
        => await db.Clients.FindAsync(new object?[] { id }, ct);
}
