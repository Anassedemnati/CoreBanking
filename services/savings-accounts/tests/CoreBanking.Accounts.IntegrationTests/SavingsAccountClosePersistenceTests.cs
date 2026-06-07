using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.Oracle;

namespace CoreBanking.Accounts.IntegrationTests;

public sealed class SavingsAccountClosePersistenceTests : IAsyncLifetime
{
    // Mirrors SavingsTransactionsPersistenceTests: same image as docker-compose's oracle-free
    // service, connecting as the SAVINGS user so the DbContext's HasDefaultSchema("SAVINGS")
    // resolves to the connecting user's own schema. See that test for the full rationale.
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
            // Testcontainers.Oracle 3.10 hard-codes SERVICE_NAME=XEPDB1; gvenzl/oracle-free's
            // PDB is FREEPDB1, so patch the service name before handing the string to EF.
            .UseOracle(_oracle.GetConnectionString().Replace("XEPDB1", "FREEPDB1"))
            .Options;

    [Fact]
    public async Task Close_with_sweep_persists_closed_status_and_closedon()
    {
        var today = new DateOnly(2026, 6, 7);
        var closedOn = new DateOnly(2026, 3, 15);
        Guid accountId;

        await using (var ctx = new SavingsAccountsWriteDbContext(Options))
        {
            await ctx.Database.MigrateAsync();

            var account = SavingsAccount.SubmitApplication(
                Guid.NewGuid(), Guid.NewGuid(), "SA-CLOSE-IT", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
            account.Approve(new DateOnly(2026, 1, 1));
            account.Activate(new DateOnly(2026, 1, 1));
            account.Deposit(new DateOnly(2026, 1, 10), 1000m, today);
            account.Close(closedOn, withdrawBalance: true, today);
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

            loaded.Status.Should().Be(SavingsAccountStatus.Closed);
            loaded.ClosedOn.Should().Be(closedOn);
            loaded.AccountBalance.Should().Be(0m);
            loaded.Transactions.Should().Contain(t =>
                t.Type == SavingsTransactionType.Withdrawal && t.Amount == 1000m);
        }
    }
}
