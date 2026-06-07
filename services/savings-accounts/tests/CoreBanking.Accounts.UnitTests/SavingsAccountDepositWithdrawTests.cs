using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Events;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests;

public sealed class SavingsAccountDepositWithdrawTests
{
    private static readonly DateOnly Today = new(2026, 6, 7);

    private static SavingsAccount MakeActive(DateOnly? activatedOn = null)
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        a.Approve(new DateOnly(2026, 1, 1));
        a.Activate(activatedOn ?? new DateOnly(2026, 1, 1));
        a.ClearDomainEvents();
        return a;
    }

    [Fact]
    public void Deposit_on_active_account_adds_transaction_updates_balance_and_raises_event()
    {
        var a = MakeActive();

        var txId = a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);

        a.AccountBalance.Should().Be(1000m);
        a.Transactions.Should().ContainSingle(t =>
            t.Id == txId && t.Type == SavingsTransactionType.Deposit &&
            t.Amount == 1000m && t.RunningBalance == 1000m);
        a.DomainEvents.OfType<SavingsDeposited>().Should().ContainSingle()
            .Which.BalanceAfter.Should().Be(1000m);
    }

    [Fact]
    public void Deposit_on_non_active_account_throws()
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));

        var act = () => a.Deposit(new DateOnly(2026, 1, 10), 100m, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.notactive");
    }

    [Fact]
    public void Deposit_with_future_date_throws()
    {
        var a = MakeActive();
        var act = () => a.Deposit(Today.AddDays(1), 100m, Today);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.future");
    }

    [Fact]
    public void Deposit_before_activation_date_throws()
    {
        var a = MakeActive(activatedOn: new DateOnly(2026, 2, 1));
        var act = () => a.Deposit(new DateOnly(2026, 1, 15), 100m, Today);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.beforeactivation");
    }

    [Fact]
    public void Deposit_with_non_positive_amount_throws()
    {
        var a = MakeActive();
        var act = () => a.Deposit(new DateOnly(2026, 1, 10), 0m, Today);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.amount.invalid");
    }

    [Fact]
    public void Backdated_deposit_reorders_timeline_and_recomputes_running_balances()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 3, 10), 500m, Today);
        a.Deposit(new DateOnly(2026, 2, 1), 200m, Today); // backdated

        var ordered = a.Transactions.OrderBy(t => t.TransactionDate).ToList();
        ordered[0].RunningBalance.Should().Be(200m);  // Feb 1
        ordered[1].RunningBalance.Should().Be(700m);  // Mar 10 recomputed
        a.AccountBalance.Should().Be(700m);
    }

    [Fact]
    public void Transaction_credit_debit_semantics_follow_fineract_type_ids()
    {
        ((int)SavingsTransactionType.Deposit).Should().Be(1);
        ((int)SavingsTransactionType.Withdrawal).Should().Be(2);
        ((int)SavingsTransactionType.InterestPosting).Should().Be(3);

        var deposit = SavingsAccountTransaction.Create(
            Guid.NewGuid(), SavingsTransactionType.Deposit, new DateOnly(2026, 1, 5), 100m, 1);
        var withdrawal = SavingsAccountTransaction.Create(
            Guid.NewGuid(), SavingsTransactionType.Withdrawal, new DateOnly(2026, 1, 6), 40m, 2);
        var interest = SavingsAccountTransaction.Create(
            Guid.NewGuid(), SavingsTransactionType.InterestPosting, new DateOnly(2026, 1, 31), 1.25m, 3);

        deposit.IsCredit.Should().BeTrue();
        withdrawal.IsCredit.Should().BeFalse();
        interest.IsCredit.Should().BeTrue();
        deposit.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SubmitApplication_snapshots_interest_settings_with_defaults()
    {
        var account = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0002", "USD", 2, 5.0m, new DateOnly(2026, 6, 6));

        account.Compounding.Should().Be(InterestCompoundingPeriod.Monthly);
        account.PostingPeriod.Should().Be(InterestPostingPeriod.Monthly);
        account.DaysInYear.Should().Be(DaysInYearType.Days365);
        account.AccountBalance.Should().Be(0m);
        account.InterestPostedTillDate.Should().BeNull();
        account.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void WithdrawMoney_with_sufficient_balance_succeeds_and_raises_event()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);

        var txId = a.WithdrawMoney(new DateOnly(2026, 1, 20), 400m, Today);

        a.AccountBalance.Should().Be(600m);
        a.Transactions.Should().Contain(t =>
            t.Id == txId && t.Type == SavingsTransactionType.Withdrawal &&
            t.Amount == 400m && t.RunningBalance == 600m);
        a.DomainEvents.OfType<SavingsWithdrawn>().Should().ContainSingle()
            .Which.BalanceAfter.Should().Be(600m);
    }

    [Fact]
    public void WithdrawMoney_exceeding_balance_throws_insufficient()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 100m, Today);

        var act = () => a.WithdrawMoney(new DateOnly(2026, 1, 20), 100.01m, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.balance.insufficient");
        a.AccountBalance.Should().Be(100m);
        a.Transactions.Should().HaveCount(1); // failed withdrawal left no trace
    }

    [Fact]
    public void Backdated_withdrawal_that_would_overdraw_past_timeline_throws()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 100m, Today);
        a.Deposit(new DateOnly(2026, 3, 1), 1000m, Today);

        // balance today is 1100, but on Feb 1 it was only 100
        var act = () => a.WithdrawMoney(new DateOnly(2026, 2, 1), 500m, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.balance.insufficient");
        a.AccountBalance.Should().Be(1100m);
        a.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public void WithdrawMoney_on_zero_balance_account_throws_insufficient()
    {
        var a = MakeActive();

        var act = () => a.WithdrawMoney(new DateOnly(2026, 1, 10), 1m, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.balance.insufficient");
        a.Transactions.Should().BeEmpty();
    }
}
