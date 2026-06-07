using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record DepositToSavingsAccountCommand(
    Guid AccountId,
    DateOnly TransactionDate,
    decimal Amount) : ICommand<Guid>;

public sealed class DepositToSavingsAccountValidator : AbstractValidator<DepositToSavingsAccountCommand>
{
    public DepositToSavingsAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class DepositToSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IDateTimeProvider dateTime)
    : ICommandHandler<DepositToSavingsAccountCommand, Guid>
{
    public async ValueTask<Guid> Handle(DepositToSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);

        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);
        var txId = account.Deposit(cmd.TransactionDate, cmd.Amount, today);

        await uow.SaveChangesAsync(ct);
        return txId;
    }
}
