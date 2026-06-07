namespace CoreBanking.Products.Application.Products;

public sealed record SavingsProductDto(
    Guid Id,
    string Name,
    string ShortName,
    string CurrencyCode,
    int CurrencyDecimalPlaces,
    decimal NominalAnnualRate,
    string Status);
