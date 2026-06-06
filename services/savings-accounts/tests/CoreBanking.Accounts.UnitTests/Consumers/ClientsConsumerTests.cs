using System.Text.Json;
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.ReadModels;
using CoreBanking.Accounts.Infrastructure.Consumers;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.Clients.Infrastructure;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Consumers;

public sealed class ClientsConsumerTests
{
    private static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value);

    // -------------------------------------------------------------------------
    // ClientRegistered → upserts ClientRef with IsActive = false
    // -------------------------------------------------------------------------
    [Fact]
    public async Task ClientRegistered_upserts_ClientRef_with_IsActive_false()
    {
        var clientId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var @event = new ClientRegisteredIntegrationEvent(
            eventId, DateTimeOffset.UtcNow, 1, clientId, "Ada Lovelace", null);

        var clientRefRepo = Substitute.For<IClientRefRepository>();
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var inbox = Substitute.For<IInboxService>();
        inbox.HasProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        await ClientsConsumer.HandleEventAsync(
            nameof(ClientRegisteredIntegrationEvent),
            Serialize(@event),
            clientRefRepo, uow, inbox,
            CancellationToken.None);

        await clientRefRepo.Received(1).UpsertAsync(
            Arg.Is<ClientRef>(r =>
                r.ClientId == clientId &&
                r.DisplayName == "Ada Lovelace" &&
                r.IsActive == false &&
                r.EventVersion == 1),
            Arg.Any<CancellationToken>());
        await inbox.Received(1).MarkProcessedAsync(eventId, nameof(ClientRegisteredIntegrationEvent), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // ClientActivated → sets IsActive = true on existing ref
    // -------------------------------------------------------------------------
    [Fact]
    public async Task ClientActivated_sets_IsActive_true_on_existing_ref()
    {
        var clientId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var existing = new ClientRef { ClientId = clientId, DisplayName = "Ada", IsActive = false, EventVersion = 1 };
        var @event = new ClientActivatedIntegrationEvent(
            eventId, DateTimeOffset.UtcNow, 2, clientId, DateOnly.FromDateTime(DateTime.Today));

        var clientRefRepo = Substitute.For<IClientRefRepository>();
        clientRefRepo.FindAsync(clientId, Arg.Any<CancellationToken>()).Returns(existing);
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var inbox = Substitute.For<IInboxService>();
        inbox.HasProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        await ClientsConsumer.HandleEventAsync(
            nameof(ClientActivatedIntegrationEvent),
            Serialize(@event),
            clientRefRepo, uow, inbox,
            CancellationToken.None);

        await clientRefRepo.Received(1).UpsertAsync(
            Arg.Is<ClientRef>(r => r.ClientId == clientId && r.IsActive == true && r.EventVersion == 2),
            Arg.Any<CancellationToken>());
        await inbox.Received(1).MarkProcessedAsync(eventId, nameof(ClientActivatedIntegrationEvent), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Duplicate event (already in inbox) is skipped — idempotency
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Duplicate_ClientRegistered_is_skipped()
    {
        var clientId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var @event = new ClientRegisteredIntegrationEvent(
            eventId, DateTimeOffset.UtcNow, 1, clientId, "Ada", null);

        var clientRefRepo = Substitute.For<IClientRefRepository>();
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var inbox = Substitute.For<IInboxService>();
        inbox.HasProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(true); // already processed

        await ClientsConsumer.HandleEventAsync(
            nameof(ClientRegisteredIntegrationEvent),
            Serialize(@event),
            clientRefRepo, uow, inbox,
            CancellationToken.None);

        await clientRefRepo.DidNotReceive().UpsertAsync(Arg.Any<ClientRef>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // Unknown event type is ignored gracefully
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Unknown_event_type_is_ignored_gracefully()
    {
        var clientRefRepo = Substitute.For<IClientRefRepository>();
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var inbox = Substitute.For<IInboxService>();

        var act = async () => await ClientsConsumer.HandleEventAsync(
            "SomeOtherEventType",
            JsonSerializer.SerializeToUtf8Bytes(new { }),
            clientRefRepo, uow, inbox,
            CancellationToken.None);

        await act.Should().NotThrowAsync();
        await clientRefRepo.DidNotReceive().UpsertAsync(Arg.Any<ClientRef>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -------------------------------------------------------------------------
    // ClientActivated when ClientRef does not exist yet → skipped gracefully
    // -------------------------------------------------------------------------
    [Fact]
    public async Task ClientActivated_when_ref_not_found_is_skipped()
    {
        var clientId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var @event = new ClientActivatedIntegrationEvent(
            eventId, DateTimeOffset.UtcNow, 2, clientId, DateOnly.FromDateTime(DateTime.Today));

        var clientRefRepo = Substitute.For<IClientRefRepository>();
        clientRefRepo.FindAsync(clientId, Arg.Any<CancellationToken>()).Returns((ClientRef?)null);
        var uow = Substitute.For<ISavingsAccountUnitOfWork>();
        var inbox = Substitute.For<IInboxService>();
        inbox.HasProcessedAsync(eventId, Arg.Any<CancellationToken>()).Returns(false);

        await ClientsConsumer.HandleEventAsync(
            nameof(ClientActivatedIntegrationEvent),
            Serialize(@event),
            clientRefRepo, uow, inbox,
            CancellationToken.None);

        await clientRefRepo.DidNotReceive().UpsertAsync(Arg.Any<ClientRef>(), Arg.Any<CancellationToken>());
        await uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
