using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Interest;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Interest;

public sealed class InterestEngineTests
{
    private static readonly Guid AccountId = Guid.NewGuid();

    private static SavingsAccountTransaction Tx(DateOnly date, decimal amount, decimal runningBalance, int sequence = 1)
    {
        var tx = SavingsAccountTransaction.Create(AccountId, SavingsTransactionType.Deposit, date, amount, sequence);
        tx.RunningBalance = runningBalance;
        return tx;
    }

    [Fact]
    public void Empty_transaction_list_produces_single_zero_balance_span()
    {
        var spans = InterestEngine.BuildSpans(
            Array.Empty<SavingsAccountTransaction>(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        spans.Should().Equal(new DailyBalanceSpan(new DateOnly(2026, 1, 1), 31, 0m));
    }

    [Fact]
    public void Transactions_before_period_set_opening_balance_for_single_span()
    {
        var txs = new[] { Tx(new DateOnly(2025, 12, 15), 500m, 500m) };

        var spans = InterestEngine.BuildSpans(txs, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        spans.Should().Equal(new DailyBalanceSpan(new DateOnly(2026, 1, 1), 31, 500m));
    }

    [Fact]
    public void Transaction_exactly_on_period_start_affects_first_day_balance()
    {
        var txs = new[] { Tx(new DateOnly(2026, 1, 1), 1000m, 1000m) };

        var spans = InterestEngine.BuildSpans(txs, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        spans.Should().Equal(new DailyBalanceSpan(new DateOnly(2026, 1, 1), 31, 1000m));
    }

    [Fact]
    public void Multiple_transactions_same_day_use_last_running_balance()
    {
        // Two deposits on Jan 10: running balances 100 then 300 (deterministic Sequence tie-break)
        var txs = new[]
        {
            Tx(new DateOnly(2026, 1, 10), 100m, 100m, sequence: 1),
            Tx(new DateOnly(2026, 1, 10), 200m, 300m, sequence: 2)
        };

        var spans = InterestEngine.BuildSpans(txs, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        spans.Should().Equal(
            new DailyBalanceSpan(new DateOnly(2026, 1, 1), 9, 0m),
            new DailyBalanceSpan(new DateOnly(2026, 1, 10), 22, 300m));
    }

    [Fact]
    public void Mid_period_transaction_splits_into_two_spans_with_correct_day_counts()
    {
        var txs = new[] { Tx(new DateOnly(2026, 1, 11), 1500m, 1500m) };

        var spans = InterestEngine.BuildSpans(txs, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        spans.Should().Equal(
            new DailyBalanceSpan(new DateOnly(2026, 1, 1), 10, 0m),
            new DailyBalanceSpan(new DateOnly(2026, 1, 11), 21, 1500m));
    }
}
