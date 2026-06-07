using System.Text.Json;
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.ReadModels;
using CoreBanking.Accounts.Infrastructure.Consumers;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.Products.Contracts;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Consumers;

public sealed class ProductsConsumerTests
{
    private static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value);

    // -------------------------------------------------------------------------
    // SavingsProductCreated → upserts ProductRef with correct fields
    // -------------------------------------------------------------------------
    [Fact]
    public async Task SavingsProductCreated_event_upserts_ProductRef()
    {
        var productId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var @event = new SavingsProductCreatedIntegrationEvent(
            eventId, DateTimeOffset.UtcNow, 1, productId, "Premium Savings", "USD", 2, 3.75m);

        var productRefRepo = Substitute.For<IProductRefRepository>();
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var inbox = Substitute.For<IInboxService>();
        inbox.HasProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        await ProductsConsumer.HandleEventAsync(
            nameof(SavingsProductCreatedIntegrationEvent),
            Serialize(@event),
            productRefRepo, uow, inbox,
            CancellationToken.None);

        await productRefRepo.Received(1).UpsertAsync(
            Arg.Is<ProductRef>(r =>
                r.ProductId == productId &&
                r.Name == "Premium Savings" &&
                r.CurrencyCode == "USD" &&
                r.CurrencyDecimalPlaces == 2 &&
                r.DefaultRate == 3.75m &&
                r.EventVersion == 1),
            Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Duplicate event (already in inbox) is skipped — idempotency
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Duplicate_event_is_skipped_via_inbox()
    {
        var productId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var @event = new SavingsProductCreatedIntegrationEvent(
            eventId, DateTimeOffset.UtcNow, 1, productId, "Basic Savings", "EUR", 2, 2.50m);

        var productRefRepo = Substitute.For<IProductRefRepository>();
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var inbox = Substitute.For<IInboxService>();
        inbox.HasProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(true); // already processed

        await ProductsConsumer.HandleEventAsync(
            nameof(SavingsProductCreatedIntegrationEvent),
            Serialize(@event),
            productRefRepo, uow, inbox,
            CancellationToken.None);

        await productRefRepo.DidNotReceive().UpsertAsync(Arg.Any<ProductRef>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Unknown event type is ignored gracefully
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Unknown_event_type_is_ignored_gracefully()
    {
        var productRefRepo = Substitute.For<IProductRefRepository>();
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var inbox = Substitute.For<IInboxService>();

        var act = async () => await ProductsConsumer.HandleEventAsync(
            "SomeUnknownEventType",
            JsonSerializer.SerializeToUtf8Bytes(new { }),
            productRefRepo, uow, inbox,
            CancellationToken.None);

        await act.Should().NotThrowAsync();
        await productRefRepo.DidNotReceive().UpsertAsync(Arg.Any<ProductRef>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Successful processing → MarkProcessedAsync and SaveChangesAsync are called
    // -------------------------------------------------------------------------
    [Fact]
    public async Task SavingsProductCreated_marks_inbox_and_saves()
    {
        var productId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var @event = new SavingsProductCreatedIntegrationEvent(
            eventId, DateTimeOffset.UtcNow, 1, productId, "Flex Savings", "GBP", 2, 1.25m);

        var productRefRepo = Substitute.For<IProductRefRepository>();
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var inbox = Substitute.For<IInboxService>();
        inbox.HasProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        await ProductsConsumer.HandleEventAsync(
            nameof(SavingsProductCreatedIntegrationEvent),
            Serialize(@event),
            productRefRepo, uow, inbox,
            CancellationToken.None);

        await inbox.Received(1).MarkProcessedAsync(eventId, nameof(SavingsProductCreatedIntegrationEvent), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
