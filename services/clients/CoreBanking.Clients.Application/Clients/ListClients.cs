using Mediator;

namespace CoreBanking.Clients.Application.Clients;

public sealed record ListClientsQuery() : IQuery<IReadOnlyList<ClientDto>>;

public sealed class ListClientsHandler(IClientReadRepository readRepo)
    : IQueryHandler<ListClientsQuery, IReadOnlyList<ClientDto>>
{
    public async ValueTask<IReadOnlyList<ClientDto>> Handle(ListClientsQuery query, CancellationToken ct)
    {
        return await readRepo.ListAsync(ct);
    }
}
