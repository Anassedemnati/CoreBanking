using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Domain;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record RejectSavingsAccountCommand(Guid AccountId, DateOnly RejectedOn) : ICommand;

public sealed class RejectSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow)
    : ICommandHandler<RejectSavingsAccountCommand>
{
    public async ValueTask<Unit> Handle(RejectSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);
        account.Reject(cmd.RejectedOn);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
