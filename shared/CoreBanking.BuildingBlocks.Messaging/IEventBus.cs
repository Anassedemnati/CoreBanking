namespace CoreBanking.BuildingBlocks.Messaging;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent @event, CancellationToken ct = default);
    Task PublishRawAsync(string topic, string key, byte[] contentBytes, string typeHeader, CancellationToken ct = default);
}
