using System.Text.Json;
using CoreBanking.Clients.Contracts;
using CoreBanking.Products.Contracts;
using FluentAssertions;

namespace CoreBanking.ContractTests;

public sealed class IntegrationEventContractTests
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void ClientRegistered_serializes_required_consumer_fields()
    {
        var evt = new ClientRegisteredIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTimeOffset.UtcNow,
            Version: 1L,
            ClientId: Guid.NewGuid(),
            DisplayName: "Jane Doe",
            ExternalId: "EXT-001");

        var json = JsonSerializer.Serialize(evt, JsonOpts);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("clientId", out _).Should().BeTrue("Accounts consumer reads clientId");
        root.TryGetProperty("displayName", out _).Should().BeTrue("Accounts consumer maps displayName → DisplayName");
        root.TryGetProperty("eventId", out _).Should().BeTrue("Inbox uses eventId for deduplication");
        root.TryGetProperty("version", out _).Should().BeTrue("ClientRef.EventVersion is set from version");
    }

    [Fact]
    public void ClientActivated_serializes_required_consumer_fields()
    {
        var evt = new ClientActivatedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTimeOffset.UtcNow,
            Version: 2L,
            ClientId: Guid.NewGuid(),
            ActivationDate: DateOnly.FromDateTime(DateTime.UtcNow));

        var json = JsonSerializer.Serialize(evt, JsonOpts);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("clientId", out _).Should().BeTrue("Accounts consumer reads clientId to find existing ClientRef");
        root.TryGetProperty("eventId", out _).Should().BeTrue("Inbox uses eventId for deduplication");
    }

    [Fact]
    public void SavingsProductCreated_serializes_required_consumer_fields()
    {
        var evt = new SavingsProductCreatedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTimeOffset.UtcNow,
            Version: 1L,
            ProductId: Guid.NewGuid(),
            Name: "Basic Savings",
            CurrencyCode: "MAD",
            CurrencyDigits: 2,
            DefaultRate: 3.5m);

        var json = JsonSerializer.Serialize(evt, JsonOpts);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("productId", out _).Should().BeTrue("ProductsConsumer reads productId");
        root.TryGetProperty("currencyCode", out _).Should().BeTrue("ProductRef.CurrencyCode is set from currencyCode");
        root.TryGetProperty("currencyDigits", out _).Should().BeTrue("ProductRef.CurrencyDecimalPlaces is set from currencyDigits");
        root.TryGetProperty("defaultRate", out _).Should().BeTrue("ProductRef.DefaultRate is set from defaultRate");
        root.TryGetProperty("eventId", out _).Should().BeTrue("Inbox uses eventId for deduplication");
        root.TryGetProperty("version", out _).Should().BeTrue("ProductRef.EventVersion is set from version");
    }
}
