using CoreBanking.Clients.Application;
using CoreBanking.Clients.Application.Clients;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Clients.Infrastructure;

public sealed class ClientReadRepository(ClientsReadDbContext db) : IClientReadRepository
{
    public async Task<ClientDto?> FindDtoAsync(Guid id, CancellationToken ct = default)
    {
        var client = await db.Clients
            .Where(c => c.Id == id)
            .Select(c => new ClientDto(
                c.Id,
                c.DisplayName,
                c.ExternalId,
                c.Status.ToString(),
                c.ActivationDate))
            .FirstOrDefaultAsync(ct);
        return client;
    }
}
