namespace CoreBanking.BuildingBlocks.Messaging.Kafka;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
}
