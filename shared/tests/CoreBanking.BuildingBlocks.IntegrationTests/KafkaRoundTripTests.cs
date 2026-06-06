using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CoreBanking.BuildingBlocks.Messaging;
using CoreBanking.BuildingBlocks.Messaging.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Testcontainers.Kafka;

namespace CoreBanking.BuildingBlocks.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class KafkaRoundTripTests : IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder().Build();

    public async Task InitializeAsync() => await _kafka.StartAsync();
    public async Task DisposeAsync() => await _kafka.DisposeAsync();

    private sealed record SampleEvent(
        Guid EventId,
        DateTimeOffset OccurredOnUtc,
        long Version,
        string Payload)
        : IntegrationEvent(EventId, OccurredOnUtc, Version)
    {
        public override string Topic => "test-topic";
        public override string AggregateKey { get; } = EventId.ToString();
    }

    private KafkaEventBus CreateBus() =>
        new(Options.Create(new KafkaOptions { BootstrapServers = _kafka.GetBootstrapAddress() }));

    private static async Task CreateCompactedTopicAsync(string bootstrapServers, string topic)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
        await admin.CreateTopicsAsync(new[]
        {
            new TopicSpecification
            {
                Name = topic,
                NumPartitions = 1,
                ReplicationFactor = 1,
                Configs = new Dictionary<string, string> { { "cleanup.policy", "compact" } }
            }
        });
    }

    [Fact]
    public async Task Publish_two_events_and_consume_in_order()
    {
        var bootstrapServers = _kafka.GetBootstrapAddress();
        await CreateCompactedTopicAsync(bootstrapServers, "test-topic");

        using var bus = CreateBus();
        var id1 = Guid.CreateVersion7();
        var id2 = Guid.CreateVersion7();
        var e1 = new SampleEvent(id1, DateTimeOffset.UtcNow, 1, "first");
        var e2 = new SampleEvent(id2, DateTimeOffset.UtcNow, 2, "second");

        await bus.PublishAsync(e1);
        await bus.PublishAsync(e2);

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "test-group-roundtrip",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe("test-topic");

        var received = new List<string>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            while (received.Count < 2)
            {
                var result = consumer.Consume(cts.Token);
                if (!result.IsPartitionEOF)
                    received.Add(result.Message.Key);
            }
        }
        finally { consumer.Close(); }

        received.Should().ContainInOrder(id1.ToString(), id2.ToString());
    }

    [Fact]
    public async Task Consumer_reset_to_offset0_replays_all_events()
    {
        var bootstrapServers = _kafka.GetBootstrapAddress();
        const string topic = "test-topic-replay";
        await CreateCompactedTopicAsync(bootstrapServers, topic);

        using var bus = CreateBus();
        var id = Guid.CreateVersion7();
        var e = new SampleEvent(id, DateTimeOffset.UtcNow, 1, "replay-me");
        await bus.PublishAsync(e);

        // First consumer group reads the event
        async Task<int> ConsumeOnce(string groupId)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
            using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
            consumer.Subscribe(topic);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var count = 0;
            try
            {
                while (true)
                {
                    var r = consumer.Consume(cts.Token);
                    if (!r.IsPartitionEOF) { count++; consumer.Commit(r); break; }
                }
            }
            catch (OperationCanceledException) { }
            finally { consumer.Close(); }
            return count;
        }

        var first = await ConsumeOnce("replay-group-1");
        first.Should().Be(1);

        // Fresh group (simulates offset-0 replay) gets the same event again
        var second = await ConsumeOnce("replay-group-2");
        second.Should().Be(1);
    }
}
