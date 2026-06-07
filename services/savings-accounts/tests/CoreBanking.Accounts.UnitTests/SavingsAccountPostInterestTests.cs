using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Events;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests;

public sealed class SavingsAccountPostInterestTests
{
    private static readonly DateOnly Today = new(2026, 6, 7);

    private static SavingsAccount MakeActiveWithDeposit()
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        a.Approve(new DateOnly(2026, 1, 1));
        a.Activate(new DateOnly(2026, 1, 1));
        a.Deposit(new DateOnly(2026, 1, 1), 1000m, Today);
        a.ClearDomainEvents();
        return a;
    }

    [Fact]
    public void PostInterest_multi_period_compounds_via_posted_balance()
    {
        // CRITICAL multi-period test: period N's posting must be in the balance
        // before period N+1 is calculated.
        var a = MakeActiveWithDeposit();

        a.PostInterest(new DateOnly(2026, 3, 31), Today);

        var postings = a.Transactions
            .Where(t => t.Type == SavingsTransactionType.InterestPosting)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        postings.Should().HaveCount(3);
        // Jan: 1000 * (0.05/365) * 31 = 4.246575 → 4.25
        postings[0].TransactionDate.Should().Be(new DateOnly(2026, 1, 31));
        postings[0].Amount.Should().Be(4.25m);
        // Feb on 1004.25: * (0.05/365) * 28 = 3.851918 → 3.85
        postings[1].TransactionDate.Should().Be(new DateOnly(2026, 2, 28));
        postings[1].Amount.Should().Be(3.85m);
        // Mar on 1008.10: * (0.05/365) * 31 = 4.280972 → 4.28
        postings[2].TransactionDate.Should().Be(new DateOnly(2026, 3, 31));
        postings[2].Amount.Should().Be(4.28m);

        a.AccountBalance.Should().Be(1012.38m);
        a.InterestPostedTillDate.Should().Be(new DateOnly(2026, 3, 31));
        a.DomainEvents.OfType<SavingsInterestPosted>().Should().HaveCount(3);
    }

    [Fact]
    public void PostInterest_is_idempotent_for_same_asOf()
    {
        var a = MakeActiveWithDeposit();
        a.PostInterest(new DateOnly(2026, 3, 31), Today);
        var balanceAfterFirst = a.AccountBalance;
        a.ClearDomainEvents();

        a.PostInterest(new DateOnly(2026, 3, 31), Today);

        a.AccountBalance.Should().Be(balanceAfterFirst);
        a.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void PostInterest_skips_partial_trailing_period()
    {
        var a = MakeActiveWithDeposit();

        a.PostInterest(new DateOnly(2026, 2, 15), Today);

        a.Transactions.Count(t => t.Type == SavingsTransactionType.InterestPosting).Should().Be(1);
        a.InterestPostedTillDate.Should().Be(new DateOnly(2026, 1, 31));
    }

    [Fact]
    public void PostInterest_on_non_active_account_throws()
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));

        var act = () => a.PostInterest(new DateOnly(2026, 3, 31), Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.postinterest.notactive");
    }

    [Fact]
    public void Transaction_on_or_before_pivot_is_rejected_after_posting()
    {
        var a = MakeActiveWithDeposit();
        a.PostInterest(new DateOnly(2026, 1, 31), Today);

        var act = () => a.Deposit(new DateOnly(2026, 1, 31), 100m, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.beforepivot");
    }
}
