using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record SubmitSavingsApplicationCommand(
    Guid ClientId,
    Guid ProductId,
    string AccountNo,
    string CurrencyCode,
    int CurrencyDecimalPlaces,
    decimal NominalAnnualRate,
    DateOnly SubmittedOn) : ICommand<Guid>;

public sealed class SubmitSavingsApplicationValidator : AbstractValidator<SubmitSavingsApplicationCommand>
{
    public SubmitSavingsApplicationValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.AccountNo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CurrencyCode).Length(3);
        RuleFor(x => x.NominalAnnualRate).GreaterThanOrEqualTo(0);
    }
}

public sealed class SubmitSavingsApplicationHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IClientRefRepository clientRefRepo,
    IProductRefRepository productRefRepo)
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

        var account = SavingsAccount.SubmitApplication(
            cmd.ClientId, cmd.ProductId, cmd.AccountNo,
            cmd.CurrencyCode, cmd.CurrencyDecimalPlaces,
            cmd.NominalAnnualRate, cmd.SubmittedOn);

        repo.Add(account);
        await uow.SaveChangesAsync(ct);
        return account.Id;
    }
}
