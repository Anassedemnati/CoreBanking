using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Domain;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record ActivateSavingsAccountCommand(Guid AccountId, DateOnly ActivatedOn) : ICommand;

public sealed class ActivateSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow)
    : ICommandHandler<ActivateSavingsAccountCommand>
{
    public async ValueTask<Unit> Handle(ActivateSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);
        account.Activate(cmd.ActivatedOn);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
