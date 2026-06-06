using Mediator;
using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Clients.Application.Clients;

public sealed record GetClientByIdQuery(Guid ClientId) : IQuery<ClientDto>;

public sealed class GetClientByIdHandler(IClientReadRepository readRepo)
    : IQueryHandler<GetClientByIdQuery, ClientDto>
{
    public async ValueTask<ClientDto> Handle(GetClientByIdQuery query, CancellationToken ct)
    {
        return await readRepo.FindDtoAsync(query.ClientId, ct)
            ?? throw new NotFoundException(nameof(Domain.Client), query.ClientId);
    }
}
