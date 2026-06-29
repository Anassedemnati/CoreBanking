using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record SubmitSavingsApplicationCommand(
    Guid ClientId,
    Guid ProductId,
    string CurrencyCode,
    int CurrencyDecimalPlaces,
    decimal NominalAnnualRate,
    DateOnly SubmittedOn,
    InterestCompoundingPeriod Compounding = InterestCompoundingPeriod.Monthly,
    InterestPostingPeriod PostingPeriod = InterestPostingPeriod.Monthly,
    DaysInYearType DaysInYear = DaysInYearType.Days365) : ICommand<Guid>;

public sealed class SubmitSavingsApplicationValidator : AbstractValidator<SubmitSavingsApplicationCommand>
{
    public SubmitSavingsApplicationValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.CurrencyCode).Length(3);
        RuleFor(x => x.NominalAnnualRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Compounding).IsInEnum();
        RuleFor(x => x.PostingPeriod).IsInEnum();
        RuleFor(x => x.DaysInYear).IsInEnum();
    }
}

public sealed class SubmitSavingsApplicationHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IClientRefRepository clientRefRepo,
    IProductRefRepository productRefRepo,
    IAccountNumberGenerator accountNumberGenerator)
    : ICommandHandler<SubmitSavingsApplicationCommand, Guid>
{
    public async ValueTask<Guid> Handle(SubmitSavingsApplicationCommand cmd, CancellationToken ct)
    {
        var clientRef = await clientRefRepo.FindAsync(cmd.ClientId, ct)
            ?? throw new DomainException("account.client.notfound", $"Client {cmd.ClientId} not found.");

        if (!clientRef.IsActive)
            throw new DomainException("account.client.inactive", $"Client {cmd.ClientId} is not active.");

        _ = await productRefRepo.FindAsync(cmd.ProductId, ct)
            ?? throw new DomainException("account.product.notfound", $"Product {cmd.ProductId} not found.");

        // Account number is generated server-side from a concurrency-safe sequence,
        // never supplied by the caller.
        var accountNo = await accountNumberGenerator.NextAsync(ct);

        var account = SavingsAccount.SubmitApplication(
            cmd.ClientId, cmd.ProductId, accountNo,
            cmd.CurrencyCode, cmd.CurrencyDecimalPlaces,
            cmd.NominalAnnualRate, cmd.SubmittedOn,
            cmd.Compounding, cmd.PostingPeriod, cmd.DaysInYear);

        repo.Add(account);
        await uow.SaveChangesAsync(ct);
        return account.Id;
    }
}
