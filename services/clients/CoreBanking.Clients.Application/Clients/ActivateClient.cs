using Mediator;
using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.Clients.Domain;

namespace CoreBanking.Clients.Application.Clients;

public sealed record ActivateClientCommand(Guid ClientId, DateOnly ActivationDate) : ICommand;

public sealed class ActivateClientHandler(IClientRepository repo, IUnitOfWork uow)
    : ICommandHandler<ActivateClientCommand>
{
    public async ValueTask<Unit> Handle(ActivateClientCommand cmd, CancellationToken ct)
    {
        var client = await repo.FindAsync(cmd.ClientId, ct)
            ?? throw new NotFoundException(nameof(Client), cmd.ClientId);
        client.Activate(cmd.ActivationDate);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
