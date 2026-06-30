using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.IntegrationTests;

public sealed class SavingsAccountClosePersistenceTests : IAsyncLifetime
{
    private readonly SavingsTestDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();
    public Task DisposeAsync() => _db.DisposeAsync();

    [Fact]
    public async Task Close_with_sweep_persists_closed_status_and_closedon()
    {
        var today = new DateOnly(2026, 6, 7);
        var closedOn = new DateOnly(2026, 3, 15);
        Guid accountId;

        await using (var ctx = _db.NewDbContext())
        {
            var account = SavingsAccount.SubmitApplication(
                Guid.NewGuid(), Guid.NewGuid(), "SA-CLOSE-IT", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
            account.Approve(new DateOnly(2026, 1, 1));
            account.Activate(new DateOnly(2026, 1, 1));
            account.Deposit(new DateOnly(2026, 1, 10), 1000m, today);
            account.Close(closedOn, withdrawBalance: true, today);
            accountId = account.Id;

            ctx.SavingsAccounts.Add(account);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = _db.NewDbContext())
        {
            var loaded = await ctx.SavingsAccounts
                .Include(a => a.Transactions)
                .SingleAsync(a => a.Id == accountId);

            loaded.Status.Should().Be(SavingsAccountStatus.Closed);
            loaded.ClosedOn.Should().Be(closedOn);
            loaded.AccountBalance.Should().Be(0m);
            loaded.Transactions.Should().Contain(t =>
                t.Type == SavingsTransactionType.Withdrawal && t.Amount == 1000m);
        }
    }
}
