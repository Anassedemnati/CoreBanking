using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.IntegrationTests;

public sealed class SavingsTransactionsPersistenceTests : IAsyncLifetime
{
    private readonly SavingsTestDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();
    public Task DisposeAsync() => _db.DisposeAsync();

    [Fact]
    public async Task Deposit_withdraw_postinterest_roundtrip_through_database()
    {
        var today = new DateOnly(2026, 6, 7);
        Guid accountId;

        await using (var ctx = _db.NewDbContext())
        {
            var account = SavingsAccount.SubmitApplication(
                Guid.NewGuid(), Guid.NewGuid(), "SA-IT-001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
            account.Approve(new DateOnly(2026, 1, 1));
            account.Activate(new DateOnly(2026, 1, 1));
            account.Deposit(new DateOnly(2026, 1, 1), 1000m, today);
            account.WithdrawMoney(new DateOnly(2026, 2, 10), 200m, today);
            account.PostInterest(new DateOnly(2026, 3, 31), today);
            accountId = account.Id;

            ctx.SavingsAccounts.Add(account);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.NewDbContext())
        {
            var loaded = await ctx.SavingsAccounts
                .Include(a => a.Transactions)
                .SingleAsync(a => a.Id == accountId);

            // 1 deposit + 1 withdrawal + 3 monthly interest postings (Jan, Feb, Mar)
            loaded.Transactions.Should().HaveCount(5);
            loaded.InterestPostedTillDate.Should().Be(new DateOnly(2026, 3, 31));

            var last = loaded.Transactions
                .OrderBy(t => t.TransactionDate).ThenBy(t => t.Sequence).Last();
            loaded.AccountBalance.Should().Be(last.RunningBalance);
            loaded.AccountBalance.Should().BeGreaterThan(800m); // 1000 - 200 + interest
        }
    }
}
