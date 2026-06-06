namespace CoreBanking.BuildingBlocks.Messaging;

public abstract record IntegrationEvent(Guid EventId, DateTimeOffset OccurredOnUtc, long Version)
{
    public abstract string Topic { get; }
    public abstract string AggregateKey { get; }
}
