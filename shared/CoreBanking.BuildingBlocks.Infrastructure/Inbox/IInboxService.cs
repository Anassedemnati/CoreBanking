namespace CoreBanking.BuildingBlocks.Infrastructure;

public interface IInboxService
{
    Task<bool> HasProcessedAsync(Guid eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(Guid eventId, string type, CancellationToken ct = default);
}
