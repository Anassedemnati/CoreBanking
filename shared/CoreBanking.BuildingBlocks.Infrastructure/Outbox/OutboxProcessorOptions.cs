namespace CoreBanking.BuildingBlocks.Infrastructure;

public sealed class OutboxProcessorOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);
    public int BatchSize { get; set; } = 100;
}
