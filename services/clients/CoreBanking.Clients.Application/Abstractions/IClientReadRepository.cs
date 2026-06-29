using CoreBanking.Clients.Application.Clients;

namespace CoreBanking.Clients.Application;

public interface IClientReadRepository
{
    Task<ClientDto?> FindDtoAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ClientDto>> ListAsync(CancellationToken ct = default);
}
