using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record TransferBetweenSavingsAccountsCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    DateOnly TransferDate,
    decimal Amount,
    string Description,
    string? ClientTransferReference = null) : ICommand<Guid>;

public sealed class TransferBetweenSavingsAccountsValidator
    : AbstractValidator<TransferBetweenSavingsAccountsCommand>
{
    public TransferBetweenSavingsAccountsValidator()
    {
        RuleFor(x => x.SourceAccountId).NotEmpty();
        RuleFor(x => x.DestinationAccountId).NotEmpty();

        RuleFor(x => x.DestinationAccountId)
            .NotEqual(x => x.SourceAccountId)
            .WithErrorCode("account.transfer.from.to.same.account")
            .WithMessage("Source and destination accounts must be different.");

        RuleFor(x => x.Amount).GreaterThan(0);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public sealed class TransferBetweenSavingsAccountsHandler(
    ISavingsAccountRepository repo,
    IAccountTransferRepository transferRepo,
    ISavingsAccountUnitOfWork uow,
    IDateTimeProvider dateTime)
    : ICommandHandler<TransferBetweenSavingsAccountsCommand, Guid>
{
    public async ValueTask<Guid> Handle(TransferBetweenSavingsAccountsCommand cmd, CancellationToken ct)
    {
        // (a) Load source + destination — throw 404 for missing accounts
        var source = await repo.FindAsync(cmd.SourceAccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.SourceAccountId);

        var destination = await repo.FindAsync(cmd.DestinationAccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.DestinationAccountId);

        // (b) Idempotency check (before any mutation)
        if (cmd.ClientTransferReference is not null)
        {
            var existing = await transferRepo.FindByClientReferenceAsync(cmd.ClientTransferReference, ct);
            if (existing is not null)
            {
                if (existing.SourceAccountId == cmd.SourceAccountId
                    && existing.DestinationAccountId == cmd.DestinationAccountId
                    && existing.Amount == cmd.Amount)
                {
                    // Idempotent replay — same payload, return existing transfer id
                    return existing.Id;
                }

                throw new DomainException(
                    "account.transfer.idempotency.conflict",
                    $"A transfer with ClientTransferReference '{cmd.ClientTransferReference}' already exists " +
                    "with different parameters.");
            }
        }

        // (c) Pre-gate checks (throw DomainException => 422 before mutating either account)

        // Currency must match
        if (source.CurrencyCode != destination.CurrencyCode)
            throw new DomainException(
                "account.transfer.currency.mismatch",
                $"Source currency '{source.CurrencyCode}' does not match destination currency '{destination.CurrencyCode}'.");

        // Source must be Active
        if (source.Status != SavingsAccountStatus.Active)
            throw new DomainException(
                "account.transfer.source.notactive",
                $"Source account is not active (status: {source.Status}).");

        // Destination must be Active
        if (destination.Status != SavingsAccountStatus.Active)
            throw new DomainException(
                "account.transfer.destination.notactive",
                $"Destination account is not active (status: {destination.Status}).");

        // Source: transfer date must not be before activation
        if (cmd.TransferDate < source.ActivatedOn!.Value)
            throw new DomainException(
                "account.transfer.source.beforeactivation",
                $"Transfer date {cmd.TransferDate:yyyy-MM-dd} is before the source account's activation date {source.ActivatedOn:yyyy-MM-dd}.");

        // Destination: transfer date must not be before activation
        if (cmd.TransferDate < destination.ActivatedOn!.Value)
            throw new DomainException(
                "account.transfer.destination.beforeactivation",
                $"Transfer date {cmd.TransferDate:yyyy-MM-dd} is before the destination account's activation date {destination.ActivatedOn:yyyy-MM-dd}.");

        // Source pivot: transfer date must be strictly after the source interest posting pivot
        if (source.InterestPostedTillDate is { } sourcePivot && cmd.TransferDate <= sourcePivot)
            throw new DomainException(
                "account.transfer.source.beforepivot",
                $"Transfer date {cmd.TransferDate:yyyy-MM-dd} is on or before the source account's interest posting pivot date {sourcePivot:yyyy-MM-dd}.");

        // Destination pivot: transfer date must be strictly after the destination interest posting pivot
        if (destination.InterestPostedTillDate is { } destPivot && cmd.TransferDate <= destPivot)
            throw new DomainException(
                "account.transfer.destination.beforepivot",
                $"Transfer date {cmd.TransferDate:yyyy-MM-dd} is on or before the destination account's interest posting pivot date {destPivot:yyyy-MM-dd}.");

        // Amount precision: must not exceed the source currency's decimal places
        if (decimal.Round(cmd.Amount, source.CurrencyDecimalPlaces) != cmd.Amount)
            throw new DomainException(
                "account.transfer.amount.precision",
                $"Transfer amount has more decimal places than the currency allows ({source.CurrencyDecimalPlaces}).");

        // (d) Resolve today from the clock
        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);

        // (e) Withdraw from source FIRST — insufficient funds throws before destination mutates
        var withdrawalTxId = source.WithdrawMoney(cmd.TransferDate, cmd.Amount, today);

        // (f) Deposit into destination
        var depositTxId = destination.Deposit(cmd.TransferDate, cmd.Amount, today);

        // (g) Create the transfer link record (raises MoneyTransferred)
        var transfer = AccountTransfer.Create(
            source.Id,
            destination.Id,
            withdrawalTxId,
            depositTxId,
            cmd.Amount,
            source.CurrencyCode,
            cmd.TransferDate,
            cmd.Description,
            cmd.ClientTransferReference);

        transferRepo.Add(transfer);

        // (h) Commit everything atomically
        await uow.SaveChangesAsync(ct);

        return transfer.Id;
    }
}
