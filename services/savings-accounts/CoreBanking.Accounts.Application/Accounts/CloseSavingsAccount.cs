using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record CloseSavingsAccountCommand(
    Guid AccountId,
    DateOnly ClosedOn,
    bool WithdrawBalance = false) : ICommand;

public sealed class CloseSavingsAccountValidator : AbstractValidator<CloseSavingsAccountCommand>
{
    public CloseSavingsAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
    }
}

public sealed class CloseSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IDateTimeProvider dateTime)
    : ICommandHandler<CloseSavingsAccountCommand>
{
    public async ValueTask<Unit> Handle(CloseSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);

        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);
        account.Close(cmd.ClosedOn, cmd.WithdrawBalance, today);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
