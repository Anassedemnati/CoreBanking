using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.Products.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Products.UnitTests;

public sealed class SavingsProductTests
{
    private static InterestSettings DefaultInterestSettings() =>
        new(NominalAnnualRate: 5.0m, CompoundingPeriod: 1, PostingPeriod: 1,
            CalculationType: 1, DaysInYearType: 365);

    [Fact]
    public void Create_with_valid_args_returns_active_product_and_raises_event()
    {
        var currency = Currency.Of("USD", 2);
        var interest = DefaultInterestSettings();

        var product = SavingsProduct.Create("Basic Savings", "BASIC", currency, interest);

        product.Status.Should().Be(SavingsProductStatus.Active);
        product.Name.Should().Be("Basic Savings");
        product.ShortName.Should().Be("BASIC");
        product.Currency.Code.Should().Be("USD");
        product.DomainEvents.OfType<SavingsProductCreated>().Should().ContainSingle()
            .Which.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public void Create_with_empty_name_throws_DomainException()
    {
        var currency = Currency.Of("USD", 2);
        var interest = DefaultInterestSettings();

        var act = () => SavingsProduct.Create("", "BASIC", currency, interest);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("product.name.required");
    }

    [Fact]
    public void Create_with_empty_shortname_throws_DomainException()
    {
        var currency = Currency.Of("USD", 2);
        var interest = DefaultInterestSettings();

        var act = () => SavingsProduct.Create("Basic Savings", "", currency, interest);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("product.shortname.required");
    }

    [Fact]
    public void Currency_Of_validates_code_length()
    {
        var act = () => Currency.Of("US", 2);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("currency.code.invalid");
    }

    [Fact]
    public void Currency_Of_normalizes_code_to_uppercase()
    {
        var currency = Currency.Of("usd", 2);

        currency.Code.Should().Be("USD");
    }

    [Fact]
    public void Currency_Of_with_negative_decimal_places_throws_DomainException()
    {
        var act = () => Currency.Of("USD", -1);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("currency.decimals.invalid");
    }

    [Fact]
    public void Create_raises_SavingsProductCreated_with_correct_rate()
    {
        var currency = Currency.Of("EUR", 2);
        var interest = new InterestSettings(3.5m, 1, 1, 1, 365);

        var product = SavingsProduct.Create("Euro Savings", "EURO", currency, interest);

        var evt = product.DomainEvents.OfType<SavingsProductCreated>().Single();
        evt.DefaultRate.Should().Be(3.5m);
        evt.CurrencyCode.Should().Be("EUR");
        evt.CurrencyDigits.Should().Be(2);
    }
}
