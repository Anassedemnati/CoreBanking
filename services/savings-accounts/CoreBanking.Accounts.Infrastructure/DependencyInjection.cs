using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain.Events;
using CoreBanking.Accounts.Infrastructure.Consumers;
using CoreBanking.Accounts.Infrastructure.Events;
using CoreBanking.Accounts.Infrastructure.Inbox;
using CoreBanking.Accounts.Infrastructure.Persistence;
using CoreBanking.Accounts.Infrastructure.Services;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Messaging;
using CoreBanking.BuildingBlocks.Messaging.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreBanking.Accounts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSavingsAccountsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var primary = configuration.GetConnectionString("Primary")
            ?? throw new InvalidOperationException("ConnectionStrings:Primary is required.");
        var replica = configuration.GetConnectionString("Replica") ?? primary;

        services.AddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<IDateTimeProvider, UtcDateTimeProvider>();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<SavingsAccountsWriteDbContext>((sp, o) =>
            o.UseOracle(primary)
             .AddInterceptors(
                 sp.GetRequiredService<AuditableEntityInterceptor>(),
                 new ConvertDomainEventsToOutboxInterceptor(DomainEventToIntegrationEventMap)));

        services.AddDbContext<SavingsAccountsReadDbContext>((sp, o) =>
            o.UseOracle(replica));

        services.AddScoped<ISavingsAccountRepository, SavingsAccountRepository>();
        services.AddScoped<ISavingsAccountReadRepository, SavingsAccountReadRepository>();
        services.AddScoped<ISavingsAccountUnitOfWork, SavingsAccountUnitOfWork>();
        services.AddScoped<IClientRefRepository, ClientRefRepository>();
        services.AddScoped<IProductRefRepository, ProductRefRepository>();
        services.AddScoped<IInboxService, InboxService>();
        services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();

        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection("Kafka"));

        // Outbox → Kafka publishing for this service's own events (savings-accounts.events).
        services.AddSingleton<IEventBus, KafkaEventBus>();
        services.AddHostedService<OutboxProcessor<SavingsAccountsWriteDbContext>>();

        services.AddHostedService<ClientsConsumer>();
        services.AddHostedService<ProductsConsumer>();

        return services;
    }

    private static IntegrationEvent? DomainEventToIntegrationEventMap(IDomainEvent domainEvent)
        => domainEvent switch
        {
            SavingsAccountSubmitted e => new SavingsAccountSubmittedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.ClientId, e.ProductId),
            SavingsAccountApproved e => new SavingsAccountApprovedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.ApprovedOn),
            SavingsAccountActivated e => new SavingsAccountActivatedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.ActivatedOn),
            SavingsAccountRejected e => new SavingsAccountRejectedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.RejectedOn),
            SavingsAccountWithdrawn e => new SavingsAccountWithdrawnIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.WithdrawnOn),
            SavingsDeposited e => new SavingsDepositedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.TransactionId, e.On, e.Amount, e.BalanceAfter),
            SavingsWithdrawn e => new SavingsWithdrawnIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.TransactionId, e.On, e.Amount, e.BalanceAfter),
            SavingsInterestPosted e => new SavingsInterestPostedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.TransactionId, e.PostedThrough, e.Amount, e.BalanceAfter),
            SavingsAccountClosed e => new SavingsAccountClosedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.ClosedOn, e.BalanceAfter),
            _ => null
        };
}
