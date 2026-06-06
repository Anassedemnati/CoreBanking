using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Domain;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record ApproveSavingsAccountCommand(Guid AccountId, DateOnly ApprovedOn) : ICommand;

public sealed class ApproveSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow)
    : ICommandHandler<ApproveSavingsAccountCommand>
{
    public async ValueTask<Unit> Handle(ApproveSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);
        account.Approve(cmd.ApprovedOn);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
