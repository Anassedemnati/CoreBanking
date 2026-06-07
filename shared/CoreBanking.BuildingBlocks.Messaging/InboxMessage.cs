namespace CoreBanking.BuildingBlocks.Messaging;

public sealed class InboxMessage
{
    public Guid EventId { get; set; }       // idempotency key
    public string Type { get; set; } = default!;
    public DateTimeOffset ReceivedOnUtc { get; set; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
}
