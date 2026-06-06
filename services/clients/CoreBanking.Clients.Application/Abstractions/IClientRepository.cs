using CoreBanking.Clients.Domain;

namespace CoreBanking.Clients.Application;

public interface IClientRepository
{
    void Add(Client client);
    Task<Client?> FindAsync(Guid id, CancellationToken ct = default);
}
