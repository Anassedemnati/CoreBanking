using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.ReadModels;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Messaging.Kafka;
using CoreBanking.Clients.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreBanking.Accounts.Infrastructure.Consumers;

public sealed class ClientsConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<KafkaOptions> kafkaOptions,
    ILogger<ClientsConsumer> logger)
    : BackgroundService
{
    private const string TopicName = "clients.events";
    private const string ConsumerGroupId = "savings-accounts-clients-consumer";

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
                    var clientRefRepo = scope.ServiceProvider.GetRequiredService<IClientRefRepository>();
                    var uow = scope.ServiceProvider.GetRequiredService<ISavingsAccountUnitOfWork>();
                    var inboxService = scope.ServiceProvider.GetRequiredService<IInboxService>();

                    await HandleEventAsync(typeName, result.Message.Value, clientRefRepo, uow, inboxService, stoppingToken);
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
        IClientRefRepository clientRefRepo,
        ISavingsAccountUnitOfWork uow,
        IInboxService inboxService,
        CancellationToken ct)
    {
        switch (typeName)
        {
            case nameof(ClientRegisteredIntegrationEvent):
            {
                var e = JsonSerializer.Deserialize<ClientRegisteredIntegrationEvent>(payload)!;
                if (await inboxService.HasProcessedAsync(e.EventId, ct)) return;
                var clientRef = new ClientRef
                {
                    ClientId = e.ClientId,
                    DisplayName = e.DisplayName,
                    IsActive = false,
                    EventVersion = e.Version
                };
                await clientRefRepo.UpsertAsync(clientRef, ct);
                await inboxService.MarkProcessedAsync(e.EventId, typeName, ct);
                await uow.SaveChangesAsync(ct);
                break;
            }
            case nameof(ClientActivatedIntegrationEvent):
            {
                var e = JsonSerializer.Deserialize<ClientActivatedIntegrationEvent>(payload)!;
                if (await inboxService.HasProcessedAsync(e.EventId, ct)) return;
                var existing = await clientRefRepo.FindAsync(e.ClientId, ct);
                if (existing is null)
                {
                    // ClientRegistered hasn't arrived yet (out-of-order). Mark processed to avoid infinite retry.
                    await inboxService.MarkProcessedAsync(e.EventId, typeName, ct);
                    await uow.SaveChangesAsync(ct);
                    return;
                }
                existing.IsActive = true;
                existing.EventVersion = e.Version;
                await clientRefRepo.UpsertAsync(existing, ct);
                await inboxService.MarkProcessedAsync(e.EventId, typeName, ct);
                await uow.SaveChangesAsync(ct);
                break;
            }
        }
    }
}
