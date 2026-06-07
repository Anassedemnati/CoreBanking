using CoreBanking.BuildingBlocks.Messaging;

namespace CoreBanking.Clients.Contracts;

public sealed record ClientRegisteredIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid ClientId,
    string DisplayName,
    string? ExternalId)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "clients.events";
    public override string AggregateKey => ClientId.ToString();
}

public sealed record ClientActivatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid ClientId,
    DateOnly ActivationDate)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "clients.events";
    public override string AggregateKey => ClientId.ToString();
}
