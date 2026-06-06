using CoreBanking.BuildingBlocks.Messaging;

namespace CoreBanking.Products.Contracts;

public sealed record SavingsProductCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid ProductId,
    string Name,
    string CurrencyCode,
    int CurrencyDigits,
    decimal DefaultRate)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "products.events";
    public override string AggregateKey => ProductId.ToString();
}
