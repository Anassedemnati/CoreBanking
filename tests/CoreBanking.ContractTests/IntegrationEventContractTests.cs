using System.Text.Json;
using CoreBanking.Accounts.Infrastructure.Events;
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

    // --- Savings account per-leg events (transfer reuses Deposit/Withdrawal types) ---

    [Fact]
    public void SavingsDeposited_serializes_required_fields()
    {
        // The deposit leg of a transfer is a SavingsDepositedIntegrationEvent — same type as
        // a manual deposit. Consumers that build account statements must be able to read it.
        var evt = new SavingsDepositedIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTimeOffset.UtcNow,
            Version: 1L,
            AccountId: Guid.NewGuid(),
            TransactionId: Guid.NewGuid(),
            TransactionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Amount: 250.00m,
            BalanceAfter: 1250.00m);

        var json = JsonSerializer.Serialize(evt, JsonOpts);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("accountId", out _).Should().BeTrue("statement consumer reads accountId");
        root.TryGetProperty("transactionId", out _).Should().BeTrue("statement consumer uses transactionId for dedup / join");
        root.TryGetProperty("transactionDate", out _).Should().BeTrue("statement consumer reads value date");
        root.TryGetProperty("amount", out _).Should().BeTrue("statement consumer reads credited amount");
        root.TryGetProperty("balanceAfter", out _).Should().BeTrue("statement consumer reads running balance");
        root.TryGetProperty("eventId", out _).Should().BeTrue("eventId present for idempotency");
    }

    [Fact]
    public void SavingsWithdrawn_serializes_required_fields()
    {
        // The withdrawal leg of a transfer is a SavingsWithdrawnIntegrationEvent — same type as
        // a manual withdrawal. Per §1.1.1, transfer legs use Deposit(1)/Withdrawal(2) ids; no new type.
        var evt = new SavingsWithdrawnIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTimeOffset.UtcNow,
            Version: 1L,
            AccountId: Guid.NewGuid(),
            TransactionId: Guid.NewGuid(),
            TransactionDate: DateOnly.FromDateTime(DateTime.UtcNow),
            Amount: 250.00m,
            BalanceAfter: 750.00m);

        var json = JsonSerializer.Serialize(evt, JsonOpts);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("accountId", out _).Should().BeTrue("statement consumer reads accountId");
        root.TryGetProperty("transactionId", out _).Should().BeTrue("statement consumer uses transactionId for dedup / join");
        root.TryGetProperty("transactionDate", out _).Should().BeTrue("statement consumer reads value date");
        root.TryGetProperty("amount", out _).Should().BeTrue("statement consumer reads debited amount");
        root.TryGetProperty("balanceAfter", out _).Should().BeTrue("statement consumer reads running balance");
        root.TryGetProperty("eventId", out _).Should().BeTrue("eventId present for idempotency");
    }

    // --- MoneyTransferred correlation event ---

    [Fact]
    public void MoneyTransferred_routes_to_savings_accounts_topic()
    {
        var evt = new MoneyTransferredIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTimeOffset.UtcNow,
            Version: 1L,
            TransferId: Guid.NewGuid(),
            SourceAccountId: Guid.NewGuid(),
            DestinationAccountId: Guid.NewGuid(),
            SourceTransactionId: Guid.NewGuid(),
            DestinationTransactionId: Guid.NewGuid(),
            Amount: 250.00m,
            CurrencyCode: "MAD",
            TransferDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ClientTransferReference: "ref-001");

        evt.Topic.Should().Be("savings-accounts.events",
            because: "MoneyTransferred must land on the savings-accounts events topic");
        evt.AggregateKey.Should().Be(evt.TransferId.ToString(),
            because: "partition key is TransferId so per-transfer ordering is preserved");
    }

    [Fact]
    public void MoneyTransferred_serializes_all_correlation_fields()
    {
        var transferId = Guid.NewGuid();
        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();
        var sourceTxId = Guid.NewGuid();
        var destTxId = Guid.NewGuid();

        var evt = new MoneyTransferredIntegrationEvent(
            EventId: Guid.NewGuid(),
            OccurredOnUtc: DateTimeOffset.UtcNow,
            Version: 1L,
            TransferId: transferId,
            SourceAccountId: sourceAccountId,
            DestinationAccountId: destinationAccountId,
            SourceTransactionId: sourceTxId,
            DestinationTransactionId: destTxId,
            Amount: 250.00m,
            CurrencyCode: "MAD",
            TransferDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ClientTransferReference: null);

        var json = JsonSerializer.Serialize(evt, JsonOpts);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("transferId", out _).Should().BeTrue("consumer needs transferId to correlate the two legs");
        root.TryGetProperty("sourceAccountId", out _).Should().BeTrue("consumer needs sourceAccountId");
        root.TryGetProperty("destinationAccountId", out _).Should().BeTrue("consumer needs destinationAccountId");
        root.TryGetProperty("sourceTransactionId", out _).Should().BeTrue("consumer needs sourceTransactionId for statement join");
        root.TryGetProperty("destinationTransactionId", out _).Should().BeTrue("consumer needs destinationTransactionId for statement join");
        root.TryGetProperty("amount", out _).Should().BeTrue("consumer reads transfer amount");
        root.TryGetProperty("currencyCode", out _).Should().BeTrue("consumer reads currency");
        root.TryGetProperty("transferDate", out _).Should().BeTrue("consumer reads transfer value date");
        root.TryGetProperty("eventId", out _).Should().BeTrue("eventId present for idempotency");
    }
}
