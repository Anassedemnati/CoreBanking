using FluentValidation;
using Mediator;
using CoreBanking.Products.Domain;

namespace CoreBanking.Products.Application.Products;

public sealed record CreateSavingsProductCommand(
    string Name,
    string ShortName,
    string CurrencyCode,
    int CurrencyDecimalPlaces,
    decimal NominalAnnualRate,
    int CompoundingPeriod,
    int PostingPeriod,
    int CalculationType,
    int DaysInYearType) : ICommand<Guid>;

public sealed class CreateSavingsProductValidator : AbstractValidator<CreateSavingsProductCommand>
{
    public CreateSavingsProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ShortName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CurrencyCode).Length(3);
        RuleFor(x => x.NominalAnnualRate).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateSavingsProductHandler(
    ISavingsProductRepository repo,
    IProductUnitOfWork uow)
    : ICommandHandler<CreateSavingsProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateSavingsProductCommand cmd, CancellationToken ct)
    {
        var currency = Currency.Of(cmd.CurrencyCode, cmd.CurrencyDecimalPlaces);
        var interestSettings = new InterestSettings(
            cmd.NominalAnnualRate,
            cmd.CompoundingPeriod,
            cmd.PostingPeriod,
            cmd.CalculationType,
            cmd.DaysInYearType);

        var product = SavingsProduct.Create(cmd.Name, cmd.ShortName, currency, interestSettings);
        repo.Add(product);
        await uow.SaveChangesAsync(ct);
        return product.Id;
    }
}
