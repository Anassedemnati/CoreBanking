using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Events;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests;

public sealed class SavingsAccountCloseTests
{
    private static readonly DateOnly Today = new(2026, 6, 7);

    private static SavingsAccount MakeActive(DateOnly? activatedOn = null)
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-CLOSE", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        a.Approve(new DateOnly(2026, 1, 1));
        a.Activate(activatedOn ?? new DateOnly(2026, 1, 1));
        a.ClearDomainEvents();
        return a;
    }

    [Fact] // AC-1
    public void Close_zero_balance_no_sweep_flips_status_stamps_date_and_raises_event()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);
        a.WithdrawMoney(new DateOnly(2026, 2, 1), 1000m, Today);   // balance 0
        a.ClearDomainEvents();

        a.Close(new DateOnly(2026, 2, 1), withdrawBalance: false, Today);

        a.Status.Should().Be(SavingsAccountStatus.Closed);
        a.ClosedOn.Should().Be(new DateOnly(2026, 2, 1));
        var closedEvent = a.DomainEvents.OfType<SavingsAccountClosed>().Should().ContainSingle().Subject;
        closedEvent.BalanceAfter.Should().Be(0m);
        closedEvent.ClosedOn.Should().Be(new DateOnly(2026, 2, 1));
    }

    [Fact] // AC-11
    public void Close_empty_account_skips_last_transaction_rule()
    {
        var a = MakeActive();
        a.Close(new DateOnly(2026, 1, 1), withdrawBalance: false, Today);
        a.Status.Should().Be(SavingsAccountStatus.Closed);
    }

    [Fact] // AC-3
    public void Close_nonzero_balance_without_sweep_is_rejected()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);
        a.ClearDomainEvents();

        var act = () => a.Close(new DateOnly(2026, 3, 15), withdrawBalance: false, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.close.balance.nonzero");
        a.Status.Should().Be(SavingsAccountStatus.Active);
        a.Transactions.Should().HaveCount(1);            // no transaction added
        a.DomainEvents.Should().BeEmpty();
    }

    [Fact] // AC-4 (submitted) + AC-9 (re-close)
    public void Close_on_non_active_throws_notactive()
    {
        var submitted = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-X", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        var act = () => submitted.Close(new DateOnly(2026, 6, 1), false, Today);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.close.notactive");

        var closed = MakeActive();
        closed.Close(new DateOnly(2026, 1, 1), false, Today);
        closed.ClearDomainEvents();
        var reClose = () => closed.Close(new DateOnly(2026, 1, 1), false, Today);
        reClose.Should().Throw<DomainException>().Which.Code.Should().Be("account.close.notactive");
        closed.DomainEvents.Should().BeEmpty();          // no second event
    }

    [Fact] // AC-5
    public void Close_future_date_throws_but_today_is_allowed()
    {
        MakeActive().Invoking(a => a.Close(Today.AddDays(1), false, Today))
            .Should().Throw<DomainException>().Which.Code.Should().Be("account.close.future");

        var ok = MakeActive();
        ok.Close(Today, false, Today);                   // closedOn == today accepted
        ok.Status.Should().Be(SavingsAccountStatus.Closed);
    }

    [Fact] // AC-6
    public void Close_before_activation_throws_but_activation_date_is_allowed()
    {
        MakeActive(activatedOn: new DateOnly(2026, 1, 1))
            .Invoking(a => a.Close(new DateOnly(2025, 12, 31), false, Today))
            .Should().Throw<DomainException>().Which.Code.Should().Be("account.close.beforeactivation");

        var ok = MakeActive(activatedOn: new DateOnly(2026, 1, 1));
        ok.Close(new DateOnly(2026, 1, 1), false, Today); // closedOn == activation accepted
        ok.Status.Should().Be(SavingsAccountStatus.Closed);
    }

    [Fact] // AC-7
    public void Close_before_last_transaction_throws_but_equal_is_allowed()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 3, 10), 1000m, Today);
        a.WithdrawMoney(new DateOnly(2026, 3, 10), 1000m, Today); // balance 0, last txn Mar 10
        a.ClearDomainEvents();

        a.Invoking(x => x.Close(new DateOnly(2026, 2, 1), false, Today))
            .Should().Throw<DomainException>().Which.Code.Should().Be("account.close.afterlasttransaction");

        a.Close(new DateOnly(2026, 3, 10), false, Today); // closedOn == lastTxnDate accepted
        a.Status.Should().Be(SavingsAccountStatus.Closed);
    }

    [Fact] // AC-2
    public void Close_with_sweep_settles_to_zero_records_withdrawal_and_raises_no_withdrawn_event()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);   // balance 1000, no other txns
        a.ClearDomainEvents();

        a.Close(new DateOnly(2026, 3, 15), withdrawBalance: true, Today);

        a.AccountBalance.Should().Be(0m);
        a.Status.Should().Be(SavingsAccountStatus.Closed);
        a.Transactions.Should().Contain(t =>
            t.Type == SavingsTransactionType.Withdrawal &&
            t.Amount == 1000m && t.TransactionDate == new DateOnly(2026, 3, 15));
        a.DomainEvents.OfType<SavingsWithdrawn>().Should().BeEmpty();        // no separate event
        a.DomainEvents.OfType<SavingsAccountClosed>().Should().ContainSingle()
            .Which.BalanceAfter.Should().Be(0m);
    }

    [Fact] // AC-8 — pivot exemption: post interest then close same day
    public void Close_with_sweep_on_pivot_date_succeeds()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 1), 1000m, Today);
        a.PostInterest(new DateOnly(2026, 1, 31), Today);     // pivot = Jan 31, balance 1004.25
        a.InterestPostedTillDate.Should().Be(new DateOnly(2026, 1, 31));
        a.ClearDomainEvents();

        // Contrast (load-bearing): the PUBLIC WithdrawMoney on the same pivot date is
        // STILL rejected by EnsureTransactionAllowed's pivot guard. This proves the
        // close-settle exemption is a genuine exemption, not an absent guard — without
        // this assertion the success-only test below would pass even if the pivot guard
        // were deleted entirely.
        a.Invoking(x => x.WithdrawMoney(new DateOnly(2026, 1, 31), 1004.25m, Today))
            .Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.beforepivot");

        // closedOn == lastTxnDate (Jan 31) == pivot. The internal settle is pivot-exempt.
        a.Close(new DateOnly(2026, 1, 31), withdrawBalance: true, Today);

        a.AccountBalance.Should().Be(0m);
        a.Status.Should().Be(SavingsAccountStatus.Closed);
    }

    [Fact] // Edge: withdrawBalance:true on an already-zero account is a no-op (no spurious transaction)
    public void Close_with_sweep_on_zero_balance_adds_no_withdrawal()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);
        a.WithdrawMoney(new DateOnly(2026, 2, 1), 1000m, Today);   // balance 0, last txn Feb 1
        a.ClearDomainEvents();
        var txCountBefore = a.Transactions.Count;

        a.Close(new DateOnly(2026, 2, 1), withdrawBalance: true, Today);

        a.Status.Should().Be(SavingsAccountStatus.Closed);
        a.AccountBalance.Should().Be(0m);
        a.Transactions.Should().HaveCount(txCountBefore);                 // AccountBalance > 0 guard => no sweep
        a.DomainEvents.OfType<SavingsWithdrawn>().Should().BeEmpty();
        a.DomainEvents.OfType<SavingsAccountClosed>().Should().ContainSingle()
            .Which.BalanceAfter.Should().Be(0m);
    }
}
