namespace CoreBanking.BuildingBlocks.Messaging;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Topic { get; set; } = default!;
    public string AggregateKey { get; set; } = default!;
    public string Content { get; set; } = default!;   // JSON
    public DateTimeOffset OccurredOnUtc { get; set; }
    public DateTimeOffset? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
