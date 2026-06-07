using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Domain;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record WithdrawSavingsApplicationCommand(Guid AccountId, DateOnly WithdrawnOn) : ICommand;

public sealed class WithdrawSavingsApplicationHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow)
    : ICommandHandler<WithdrawSavingsApplicationCommand>
{
    public async ValueTask<Unit> Handle(WithdrawSavingsApplicationCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);
        account.Withdraw(cmd.WithdrawnOn);
        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
