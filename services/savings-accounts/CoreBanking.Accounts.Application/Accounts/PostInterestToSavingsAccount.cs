using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record PostInterestToSavingsAccountCommand(
    Guid AccountId,
    DateOnly AsOf) : ICommand;

public sealed class PostInterestToSavingsAccountValidator : AbstractValidator<PostInterestToSavingsAccountCommand>
{
    public PostInterestToSavingsAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
    }
}

public sealed class PostInterestToSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IDateTimeProvider dateTime)
    : ICommandHandler<PostInterestToSavingsAccountCommand>
{
    public async ValueTask<Unit> Handle(PostInterestToSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);

        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);
        account.PostInterest(cmd.AsOf, today);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
