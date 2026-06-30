using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Events;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;

namespace CoreBanking.Accounts.UnitTests;

public sealed class AccountTransferTests
{
    private static readonly DateOnly TransferDate = new(2026, 6, 29);

    private static AccountTransfer MakeTransfer(
        string? description = null,
        string? clientRef = null)
    {
        return AccountTransfer.Create(
            sourceAccountId: Guid.NewGuid(),
            destinationAccountId: Guid.NewGuid(),
            sourceTransactionId: Guid.NewGuid(),
            destinationTransactionId: Guid.NewGuid(),
            amount: 250.00m,
            currencyCode: "USD",
            transferDate: TransferDate,
            description: description ?? "Rent payment",
            clientTransferReference: clientRef);
    }

    [Fact]
    public void Create_sets_all_fields_correctly()
    {
        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();
        var sourceTxId = Guid.NewGuid();
        var destTxId = Guid.NewGuid();
        const decimal amount = 250.00m;
        const string currency = "USD";
        const string description = "Rent payment";
        const string clientRef = "ref-001";

        var transfer = AccountTransfer.Create(
            sourceAccountId: sourceAccountId,
            destinationAccountId: destinationAccountId,
            sourceTransactionId: sourceTxId,
            destinationTransactionId: destTxId,
            amount: amount,
            currencyCode: currency,
            transferDate: TransferDate,
            description: description,
            clientTransferReference: clientRef);

        transfer.Id.Should().NotBe(Guid.Empty);
        transfer.SourceAccountId.Should().Be(sourceAccountId);
        transfer.DestinationAccountId.Should().Be(destinationAccountId);
        transfer.SourceTransactionId.Should().Be(sourceTxId);
        transfer.DestinationTransactionId.Should().Be(destTxId);
        transfer.Amount.Should().Be(amount);
        transfer.CurrencyCode.Should().Be(currency);
        transfer.TransferDate.Should().Be(TransferDate);
        transfer.Description.Should().Be(description);
        transfer.ClientTransferReference.Should().Be(clientRef);
    }

    [Fact]
    public void Create_raises_MoneyTransferred_domain_event()
    {
        var transfer = MakeTransfer();

        var evt = transfer.DomainEvents.OfType<MoneyTransferred>().Should().ContainSingle().Subject;
        evt.TransferId.Should().Be(transfer.Id);
        evt.SourceAccountId.Should().Be(transfer.SourceAccountId);
        evt.DestinationAccountId.Should().Be(transfer.DestinationAccountId);
        evt.SourceTransactionId.Should().Be(transfer.SourceTransactionId);
        evt.DestinationTransactionId.Should().Be(transfer.DestinationTransactionId);
        evt.Amount.Should().Be(transfer.Amount);
        evt.CurrencyCode.Should().Be(transfer.CurrencyCode);
        evt.TransferDate.Should().Be(transfer.TransferDate);
        evt.ClientTransferReference.Should().Be(transfer.ClientTransferReference);
    }

    [Fact]
    public void Create_with_null_client_reference_is_allowed()
    {
        var transfer = MakeTransfer(clientRef: null);
        transfer.ClientTransferReference.Should().BeNull();
    }

    [Fact]
    public void Create_with_description_of_exactly_100_chars_succeeds()
    {
        var description = new string('x', 100);
        var act = () => MakeTransfer(description: description);
        act.Should().NotThrow();
    }

    [Fact]
    public void Create_with_description_longer_than_100_chars_throws_DomainException()
    {
        var description = new string('x', 101);

        var act = () => MakeTransfer(description: description);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("account.transfer.description.toolong");
    }

    [Fact]
    public void Create_with_description_of_200_chars_throws_DomainException()
    {
        var description = new string('x', 200);

        var act = () => MakeTransfer(description: description);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be("account.transfer.description.toolong");
    }
}
