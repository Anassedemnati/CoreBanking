using CoreBanking.BuildingBlocks.Messaging;

namespace CoreBanking.Accounts.Infrastructure.Events;

public sealed record MoneyTransferredIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid TransferId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    Guid SourceTransactionId,
    Guid DestinationTransactionId,
    decimal Amount,
    string CurrencyCode,
    DateOnly TransferDate,
    string? ClientTransferReference)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => TransferId.ToString();
}
