using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Messaging;
using CoreBanking.Clients.Application;
using CoreBanking.Clients.Contracts;
using CoreBanking.Clients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreBanking.Clients.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddClientsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var primary = configuration.GetConnectionString("Primary")
            ?? throw new InvalidOperationException("ConnectionStrings:Primary is required.");
        var replica = configuration.GetConnectionString("Replica") ?? primary;

        services.AddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<IDateTimeProvider, UtcDateTimeProvider>();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<ClientsWriteDbContext>((sp, o) =>
            o.UseOracle(primary)
             .AddInterceptors(
                 sp.GetRequiredService<AuditableEntityInterceptor>(),
                 new ConvertDomainEventsToOutboxInterceptor(DomainEventToIntegrationEventMap)));

        services.AddDbContext<ClientsReadDbContext>((sp, o) =>
            o.UseOracle(replica));

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IClientReadRepository, ClientReadRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    private static IntegrationEvent? DomainEventToIntegrationEventMap(IDomainEvent domainEvent)
        => domainEvent switch
        {
            ClientRegistered e => new ClientRegisteredIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.ClientId, e.DisplayName, e.ExternalId),
            ClientActivated e => new ClientActivatedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.ClientId, e.ActivationDate),
            _ => null
        };
}
