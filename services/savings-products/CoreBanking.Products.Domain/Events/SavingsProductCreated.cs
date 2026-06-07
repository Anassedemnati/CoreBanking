using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Products.Domain;

public sealed record SavingsProductCreated(
    Guid ProductId,
    string Name,
    string CurrencyCode,
    int CurrencyDigits,
    decimal DefaultRate) : IDomainEvent;
