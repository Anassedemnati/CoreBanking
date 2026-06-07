using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreBanking.BuildingBlocks.Messaging.Kafka;

public abstract class KafkaConsumerBackgroundService<TEvent>(
    IOptions<KafkaOptions> options,
    ILogger<KafkaConsumerBackgroundService<TEvent>> logger,
    string groupId,
    IEnumerable<string> topics)
    : BackgroundService
    where TEvent : IntegrationEvent
{
    protected abstract Task HandleAsync(TEvent @event, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(topics);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, byte[]>? result = null;
                try
                {
                    result = consumer.Consume(stoppingToken);
                    if (result.IsPartitionEOF) continue;

                    var @event = System.Text.Json.JsonSerializer.Deserialize<TEvent>(
                        result.Message.Value);

                    if (@event is not null)
                        await HandleAsync(@event, stoppingToken);

                    consumer.Commit(result);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing Kafka message from {Topic}", result?.Topic);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
