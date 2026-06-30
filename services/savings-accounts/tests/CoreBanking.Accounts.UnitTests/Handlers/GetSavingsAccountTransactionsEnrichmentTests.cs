using CoreBanking.Accounts.Application.Accounts;
using FluentAssertions;

namespace CoreBanking.Accounts.UnitTests.Handlers;

/// <summary>
/// Verifies the pure in-memory enrichment logic that attaches transfer metadata
/// to savings-account transaction DTOs after the EF queries have completed.
/// No database / Docker required — tests call the static helper directly.
/// </summary>
public sealed class GetSavingsAccountTransactionsEnrichmentTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static SavingsTransactionDto MakeDeposit(Guid id, decimal amount = 500m)
        => new(id, 1, "Deposit", new DateOnly(2026, 6, 1), amount, amount);

    private static SavingsTransactionDto MakeWithdrawal(Guid id, decimal amount = 200m, decimal balance = 300m)
        => new(id, 2, "Withdrawal", new DateOnly(2026, 6, 15), amount, balance);

    // -------------------------------------------------------------------------
    // Case 1: Manual deposit — Transfer block must be null
    // -------------------------------------------------------------------------

    [Fact]
    public void Manual_deposit_produces_null_transfer_block()
    {
        var txId = Guid.NewGuid();
        var txns = new List<SavingsTransactionDto> { MakeDeposit(txId) };

        // No matching transfer records
        var transferLegs = new List<TransferLegInfo>();

        var result = TransactionEnricher.Enrich(txns, transferLegs);

        result.Should().HaveCount(1);
        result[0].Transfer.Should().BeNull("manual deposit carries no transfer metadata");
    }

    // -------------------------------------------------------------------------
    // Case 2: Source (withdrawal) leg — Direction="out", counterparty = destination
    // -------------------------------------------------------------------------

    [Fact]
    public void Withdrawal_that_is_transfer_source_gets_direction_out_with_counterparty()
    {
        var withdrawalTxId = Guid.NewGuid();
        var depositTxId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();
        const string destinationAccountNo = "SA-002";

        var txns = new List<SavingsTransactionDto> { MakeWithdrawal(withdrawalTxId) };

        var transferLegs = new List<TransferLegInfo>
        {
            new(
                TransferId: transferId,
                SourceTransactionId: withdrawalTxId,
                DestinationTransactionId: depositTxId,
                SourceAccountId: Guid.NewGuid(),
                DestinationAccountId: destinationAccountId,
                DestinationAccountNo: destinationAccountNo,
                SourceAccountNo: "SA-001")
        };

        var result = TransactionEnricher.Enrich(txns, transferLegs);

        result.Should().HaveCount(1);
        var info = result[0].Transfer;
        info.Should().NotBeNull();
        info!.TransferId.Should().Be(transferId);
        info.Direction.Should().Be("out", "withdrawal on the source = money going out");
        info.CounterpartyAccountId.Should().Be(destinationAccountId);
        info.CounterpartyAccountNo.Should().Be(destinationAccountNo);
    }

    // -------------------------------------------------------------------------
    // Case 3: Destination (deposit) leg — Direction="in", counterparty = source
    // -------------------------------------------------------------------------

    [Fact]
    public void Deposit_that_is_transfer_destination_gets_direction_in_with_counterparty()
    {
        var depositTxId = Guid.NewGuid();
        var withdrawalTxId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var sourceAccountId = Guid.NewGuid();
        const string sourceAccountNo = "SA-001";

        var txns = new List<SavingsTransactionDto> { MakeDeposit(depositTxId) };

        var transferLegs = new List<TransferLegInfo>
        {
            new(
                TransferId: transferId,
                SourceTransactionId: withdrawalTxId,
                DestinationTransactionId: depositTxId,
                SourceAccountId: sourceAccountId,
                DestinationAccountId: Guid.NewGuid(),
                DestinationAccountNo: "SA-002",
                SourceAccountNo: sourceAccountNo)
        };

        var result = TransactionEnricher.Enrich(txns, transferLegs);

        result.Should().HaveCount(1);
        var info = result[0].Transfer;
        info.Should().NotBeNull();
        info!.TransferId.Should().Be(transferId);
        info.Direction.Should().Be("in", "deposit on the destination = money coming in");
        info.CounterpartyAccountId.Should().Be(sourceAccountId);
        info.CounterpartyAccountNo.Should().Be(sourceAccountNo);
    }

    // -------------------------------------------------------------------------
    // Case 4: Mixed list — some manual, some transfer legs
    // -------------------------------------------------------------------------

    [Fact]
    public void Mixed_list_enriches_transfer_legs_and_leaves_manual_ones_null()
    {
        var manualDepositId = Guid.NewGuid();
        var transferWithdrawalId = Guid.NewGuid();
        var depositTxId = Guid.NewGuid();
        var transferId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();

        var txns = new List<SavingsTransactionDto>
        {
            MakeDeposit(manualDepositId, 1000m),
            MakeWithdrawal(transferWithdrawalId, 300m, 700m)
        };

        var transferLegs = new List<TransferLegInfo>
        {
            new(
                TransferId: transferId,
                SourceTransactionId: transferWithdrawalId,
                DestinationTransactionId: depositTxId,
                SourceAccountId: Guid.NewGuid(),
                DestinationAccountId: destinationAccountId,
                DestinationAccountNo: "SA-002",
                SourceAccountNo: "SA-001")
        };

        var result = TransactionEnricher.Enrich(txns, transferLegs);

        result.Should().HaveCount(2);

        var manualResult = result.Single(t => t.Id == manualDepositId);
        manualResult.Transfer.Should().BeNull();

        var transferResult = result.Single(t => t.Id == transferWithdrawalId);
        transferResult.Transfer.Should().NotBeNull();
        transferResult.Transfer!.Direction.Should().Be("out");
        transferResult.Transfer.CounterpartyAccountId.Should().Be(destinationAccountId);
    }
}
