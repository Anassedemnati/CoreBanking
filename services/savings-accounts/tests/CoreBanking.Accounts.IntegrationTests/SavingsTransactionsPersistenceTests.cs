using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.Oracle;

namespace CoreBanking.Accounts.IntegrationTests;

public sealed class SavingsTransactionsPersistenceTests : IAsyncLifetime
{
    // Same image as docker/docker-compose.yml's oracle-free service; already pulled locally.
    // App user is SAVINGS: the gvenzl image creates it at startup (APP_USER), and in Oracle a
    // schema IS a user — so connecting as SAVINGS makes the DbContext's HasDefaultSchema("SAVINGS")
    // resolve to the connecting user's own schema. Do NOT use "system": the image rejects
    // APP_USER=system (ORA-01920 conflict with the built-in SYSTEM account).
    private const string OraPassword = "TestPassword1";
    private readonly OracleContainer _oracle = new OracleBuilder()
        .WithImage("gvenzl/oracle-free:latest")
        .WithUsername("SAVINGS")
        .WithPassword(OraPassword)
        .Build();

    public Task InitializeAsync() => _oracle.StartAsync();
    public Task DisposeAsync() => _oracle.DisposeAsync().AsTask();

    private DbContextOptions<SavingsAccountsWriteDbContext> Options =>
        new DbContextOptionsBuilder<SavingsAccountsWriteDbContext>()
            // Testcontainers.Oracle 3.10 defaults to gvenzl/oracle-xe and hard-codes
            // SERVICE_NAME=XEPDB1 in GetConnectionString(). With gvenzl/oracle-free the
            // PDB is FREEPDB1, so we patch the service name before handing it to EF.
            .UseOracle(_oracle.GetConnectionString().Replace("XEPDB1", "FREEPDB1"))
            .Options;

    [Fact]
    public async Task Deposit_withdraw_postinterest_roundtrip_through_oracle()
    {
        var today = new DateOnly(2026, 6, 7);
        Guid accountId;

        await using (var ctx = new SavingsAccountsWriteDbContext(Options))
        {
            await ctx.Database.MigrateAsync();

            var account = SavingsAccount.SubmitApplication(
                Guid.NewGuid(), Guid.NewGuid(), "SA-IT-001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
            account.Approve(new DateOnly(2026, 1, 1));
            account.Activate(new DateOnly(2026, 1, 1));
            account.Deposit(new DateOnly(2026, 1, 1), 1000m, today);
            account.WithdrawMoney(new DateOnly(2026, 2, 10), 200m, today);
            account.PostInterest(new DateOnly(2026, 3, 31), today);
            account.ClearDomainEvents(); // outbox interceptor is not wired in this raw context
            accountId = account.Id;

            ctx.SavingsAccounts.Add(account);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new SavingsAccountsWriteDbContext(Options))
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
