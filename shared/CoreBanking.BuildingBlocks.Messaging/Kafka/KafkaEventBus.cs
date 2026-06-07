using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace CoreBanking.BuildingBlocks.Messaging.Kafka;

public sealed class KafkaEventBus : IEventBus, IDisposable
{
    private readonly IProducer<string, byte[]> _producer;

    public KafkaEventBus(IOptions<KafkaOptions> options)
    {
        _producer = new ProducerBuilder<string, byte[]>(new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All
        }).Build();
    }

    public async Task PublishAsync(IntegrationEvent @event, CancellationToken ct = default)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType());
        var message = new Message<string, byte[]>
        {
            Key = @event.AggregateKey,
            Value = payload,
            Headers = new Headers
            {
                { "type", Encoding.UTF8.GetBytes(@event.GetType().Name) }
            }
        };
        await _producer.ProduceAsync(@event.Topic, message, ct);
    }

    public async Task PublishRawAsync(string topic, string key, byte[] contentBytes,
        string typeHeader, CancellationToken ct = default)
    {
        var message = new Message<string, byte[]>
        {
            Key = key,
            Value = contentBytes,
            Headers = new Headers
            {
                { "type", Encoding.UTF8.GetBytes(typeHeader) }
            }
        };
        await _producer.ProduceAsync(topic, message, ct);
    }

    public void Dispose() => _producer.Dispose();
}
