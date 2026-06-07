using CoreBanking.BuildingBlocks.Messaging;

namespace CoreBanking.Accounts.Infrastructure.Events;

public sealed record SavingsAccountSubmittedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    Guid ClientId,
    Guid ProductId)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsAccountApprovedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    DateOnly ApprovedOn)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsAccountActivatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    DateOnly ActivatedOn)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsAccountRejectedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    DateOnly RejectedOn)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsAccountWithdrawnIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    DateOnly WithdrawnOn)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsDepositedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    Guid TransactionId,
    DateOnly TransactionDate,
    decimal Amount,
    decimal BalanceAfter)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsAccountClosedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    DateOnly ClosedOn,
    decimal BalanceAfter)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsWithdrawnIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    Guid TransactionId,
    DateOnly TransactionDate,
    decimal Amount,
    decimal BalanceAfter)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsInterestPostedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    Guid TransactionId,
    DateOnly PostedThrough,
    decimal Amount,
    decimal BalanceAfter)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}
