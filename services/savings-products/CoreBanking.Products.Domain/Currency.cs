using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Products.Domain;

public sealed record Currency(string Code, int DecimalPlaces)
{
    public static Currency Of(string code, int decimalPlaces)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            throw new DomainException("currency.code.invalid", "Currency code must be exactly 3 characters.");
        if (decimalPlaces < 0 || decimalPlaces > 8)
            throw new DomainException("currency.decimals.invalid", "Decimal places must be 0–8.");
        return new Currency(code.ToUpperInvariant(), decimalPlaces);
    }
}
