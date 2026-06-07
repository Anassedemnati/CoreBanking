using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record WithdrawFromSavingsAccountCommand(
    Guid AccountId,
    DateOnly TransactionDate,
    decimal Amount) : ICommand<Guid>;

public sealed class WithdrawFromSavingsAccountValidator : AbstractValidator<WithdrawFromSavingsAccountCommand>
{
    public WithdrawFromSavingsAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class WithdrawFromSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IDateTimeProvider dateTime)
    : ICommandHandler<WithdrawFromSavingsAccountCommand, Guid>
{
    public async ValueTask<Guid> Handle(WithdrawFromSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);

        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);
        var txId = account.WithdrawMoney(cmd.TransactionDate, cmd.Amount, today);

        await uow.SaveChangesAsync(ct);
        return txId;
    }
}
