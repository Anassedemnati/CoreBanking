using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.ReadModels;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Messaging.Kafka;
using CoreBanking.Products.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreBanking.Accounts.Infrastructure.Consumers;

public sealed class ProductsConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<ProductsConsumer> logger)
    : BackgroundService
{
    private const string TopicName = "products.events";
    private const string ConsumerGroupId = "savings-accounts-products-consumer";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            GroupId = ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(TopicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, byte[]>? result = null;
                try
                {
                    result = consumer.Consume(stoppingToken);
                    if (result.IsPartitionEOF) continue;

                    var typeHeader = result.Message.Headers
                        .FirstOrDefault(h => h.Key == "type");

                    if (typeHeader is null)
                    {
                        logger.LogWarning("Message on {Topic} missing 'type' header — skipped.", TopicName);
                        consumer.Commit(result);
                        continue;
                    }

                    var typeName = Encoding.UTF8.GetString(typeHeader.GetValueBytes());

                    await using var scope = scopeFactory.CreateAsyncScope();
                    var productRefRepo = scope.ServiceProvider.GetRequiredService<IProductRefRepository>();
                    var uow = scope.ServiceProvider.GetRequiredService<ISavingsAccountUnitOfWork>();
                    var inboxService = scope.ServiceProvider.GetRequiredService<IInboxService>();

                    await HandleEventAsync(typeName, result.Message.Value, productRefRepo, uow, inboxService, stoppingToken);
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

    internal static async Task HandleEventAsync(
        string typeName,
        byte[] payload,
        IProductRefRepository productRefRepo,
        ISavingsAccountUnitOfWork uow,
        IInboxService inboxService,
        CancellationToken ct)
    {
        switch (typeName)
        {
            case nameof(SavingsProductCreatedIntegrationEvent):
            {
                var e = JsonSerializer.Deserialize<SavingsProductCreatedIntegrationEvent>(payload)!;
                if (await inboxService.HasProcessedAsync(e.EventId, ct)) return;
                var productRef = new ProductRef
                {
                    ProductId = e.ProductId,
                    Name = e.Name,
                    CurrencyCode = e.CurrencyCode,
                    CurrencyDecimalPlaces = e.CurrencyDigits,
                    DefaultRate = e.DefaultRate,
                    EventVersion = e.Version
                };
                await productRefRepo.UpsertAsync(productRef, ct);
                await inboxService.MarkProcessedAsync(e.EventId, typeName, ct);
                await uow.SaveChangesAsync(ct);
                break;
            }
        }
    }
}
