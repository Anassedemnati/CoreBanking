using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Messaging;
using CoreBanking.Products.Application;
using CoreBanking.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoreBanking.Products.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var primary = configuration.GetConnectionString("Primary")
            ?? throw new InvalidOperationException("ConnectionStrings:Primary is required.");
        var replica = configuration.GetConnectionString("Replica") ?? primary;

        services.AddScoped<ICurrentUser, SystemCurrentUser>();
        services.AddScoped<IDateTimeProvider, UtcDateTimeProvider>();
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<SavingsProductsWriteDbContext>((sp, o) =>
            o.UseOracle(primary)
             .AddInterceptors(
                 sp.GetRequiredService<AuditableEntityInterceptor>(),
                 new ConvertDomainEventsToOutboxInterceptor(DomainEventToIntegrationEventMap)));

        services.AddDbContext<SavingsProductsReadDbContext>((sp, o) =>
            o.UseOracle(replica));

        services.AddScoped<ISavingsProductRepository, SavingsProductRepository>();
        services.AddScoped<ISavingsProductReadRepository, SavingsProductReadRepository>();
        services.AddScoped<IProductUnitOfWork, ProductUnitOfWork>();

        return services;
    }

    private static IntegrationEvent? DomainEventToIntegrationEventMap(IDomainEvent domainEvent)
        => domainEvent switch
        {
            SavingsProductCreated e => new SavingsProductCreatedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.ProductId, e.Name, e.CurrencyCode, e.CurrencyDigits, e.DefaultRate),
            _ => null
        };
}
