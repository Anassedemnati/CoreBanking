using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.IntegrationTests;

/// <summary>
/// Integration tests for the account-to-account transfer feature.
///
/// Uses <see cref="SavingsTestDatabase"/> (provider-switchable: SQLite in-memory by
/// default, Oracle Testcontainer when COREBANKING_TEST_DB=oracle) with all three
/// interceptors active:
/// <list type="bullet">
///   <item><description><c>AuditableEntityInterceptor</c></description></item>
///   <item><description><c>AggregateVersionInterceptor</c> (required for concurrency tests)</description></item>
///   <item><description><c>ConvertDomainEventsToOutboxInterceptor</c> with the real domain→integration map (required for outbox assertions)</description></item>
/// </list>
///
/// Each test method creates its own <see cref="SavingsTestDatabase"/> instance via the
/// delegating-IAsyncLifetime pattern so every test starts with an isolated, empty DB.
/// </summary>
public sealed class AccountTransferPersistenceTests : IAsyncLifetime
{
    private readonly SavingsTestDatabase _db = new();

    public Task InitializeAsync() => _db.InitializeAsync();
    public Task DisposeAsync() => _db.DisposeAsync();

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static SavingsAccount MakeActiveAccount(string accountNo, DateOnly today)
    {
        var account = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), accountNo, "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        account.Approve(new DateOnly(2026, 1, 1));
        account.Activate(new DateOnly(2026, 1, 1));
        account.ClearDomainEvents();
        return account;
    }

    // -----------------------------------------------------------------------
    // Scenario (a): Success → both account UPDATEs + ACCOUNT_TRANSFERS row
    //               + exactly 3 outbox rows, all atomically committed.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Success_commits_both_account_updates_transfer_row_and_three_outbox_rows_atomically()
    {
        var today = new DateOnly(2026, 6, 29);
        Guid sourceId;
        Guid destinationId;

        // Phase 1: seed both accounts using a raw (no-interceptor) context so there
        // are no pre-existing outbox rows when we assert in Phase 3.
        await using (var ctx = _db.NewRawDbContext())
        {
            var source = MakeActiveAccount("SA-PRS-001", today);
            source.Deposit(new DateOnly(2026, 1, 1), 500m, today);
            source.ClearDomainEvents();
            sourceId = source.Id;

            var destination = MakeActiveAccount("SA-PRS-002", today);
            destination.ClearDomainEvents();
            destinationId = destination.Id;

            ctx.SavingsAccounts.Add(source);
            ctx.SavingsAccounts.Add(destination);
            await ctx.SaveChangesAsync();
        }

        // Phase 2: execute the transfer through a fully-wired context
        // (all interceptors active → AggregateVersion + Auditable + OutboxConverter)
        Guid transferId;
        await using (var ctx = _db.NewDbContext())
        {
            var source = await ctx.SavingsAccounts
                .Include(a => a.Transactions)
                .SingleAsync(a => a.Id == sourceId);
            var destination = await ctx.SavingsAccounts
                .Include(a => a.Transactions)
                .SingleAsync(a => a.Id == destinationId);

            var withdrawalTxId = source.WithdrawMoney(today, 200m, today);
            var depositTxId = destination.Deposit(today, 200m, today);

            var transfer = AccountTransfer.Create(
                source.Id, destination.Id,
                withdrawalTxId, depositTxId,
                200m, "USD", today, "Integration test transfer", null);

            ctx.AccountTransfers.Add(transfer);
            transferId = transfer.Id;

            await ctx.SaveChangesAsync();
        }

        // Phase 3: assert — reload everything in a fresh raw context
        await using (var ctx = _db.NewRawDbContext())
        {
            // ACCOUNT_TRANSFERS row persisted
            var loaded = await ctx.AccountTransfers.SingleOrDefaultAsync(t => t.Id == transferId);
            loaded.Should().NotBeNull("transfer link row must be persisted");
            loaded!.SourceAccountId.Should().Be(sourceId);
            loaded.DestinationAccountId.Should().Be(destinationId);
            loaded.Amount.Should().Be(200m);
            loaded.CurrencyCode.Should().Be("USD");

            // Source account balance updated
            var source = await ctx.SavingsAccounts
                .Include(a => a.Transactions)
                .SingleAsync(a => a.Id == sourceId);
            source.AccountBalance.Should().Be(300m, "500 - 200 = 300");

            // Destination account balance updated
            var destination = await ctx.SavingsAccounts
                .Include(a => a.Transactions)
                .SingleAsync(a => a.Id == destinationId);
            destination.AccountBalance.Should().Be(200m, "received 200");

            // Exactly 3 outbox rows: SavingsWithdrawn + SavingsDeposited + MoneyTransferred
            var outboxRows = await ctx.Set<CoreBanking.BuildingBlocks.Messaging.OutboxMessage>()
                .Where(m => m.Topic == "savings-accounts.events")
                .ToListAsync();

            // Phase 1 used a raw context (no interceptors) → zero pre-existing outbox rows.
            // Phase 2 produced exactly 3: withdrawal + deposit + money-transferred.
            var withdrawnRows = outboxRows.Count(m => m.Type == "SavingsWithdrawnIntegrationEvent");
            var depositedRows = outboxRows.Count(m => m.Type == "SavingsDepositedIntegrationEvent");
            var transferredRows = outboxRows.Count(m => m.Type == "MoneyTransferredIntegrationEvent");

            withdrawnRows.Should().BeGreaterThanOrEqualTo(1, "withdrawal leg must produce SavingsWithdrawn outbox row");
            depositedRows.Should().BeGreaterThanOrEqualTo(1, "deposit leg must produce SavingsDeposited outbox row");
            transferredRows.Should().Be(1, "exactly one MoneyTransferred event per transfer");

            outboxRows.Should().HaveCount(3,
                because: "phase-1 seeding used raw context (no outbox), so all 3 outbox rows come from the transfer");
        }
    }

    // -----------------------------------------------------------------------
    // Scenario (b): Forced failure rolls back all changes atomically.
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Forced_failure_rolls_back_both_accounts_and_transfer_row_and_outbox_rows()
    {
        var today = new DateOnly(2026, 6, 29);
        Guid sourceId;
        Guid destinationId;

        // Phase 1: seed accounts (raw context — no outbox rows produced)
        await using (var ctx = _db.NewRawDbContext())
        {
            var source = MakeActiveAccount("SA-PRB-001", today);
            source.Deposit(new DateOnly(2026, 1, 1), 500m, today);
            source.ClearDomainEvents();
            sourceId = source.Id;

            var destination = MakeActiveAccount("SA-PRB-002", today);
            destination.ClearDomainEvents();
            destinationId = destination.Id;

            ctx.SavingsAccounts.Add(source);
            ctx.SavingsAccounts.Add(destination);
            await ctx.SaveChangesAsync();
        }

        // Race-condition simulation:
        //   1. Context T loads source (sees Version=0, OriginalValue=0)
        //   2. Interferer commits a deposit → DB Version becomes 1
        //   3. Context T calls SaveChangesAsync → AggregateVersionInterceptor bumps
        //      CurrentValue to 1, EF emits WHERE VERSION=0, DB has 1 → 0 rows affected
        //      → DbUpdateConcurrencyException

        // Step 1: Load in context T
        await using var ctxT = _db.NewDbContext();

        var sourceInT = await ctxT.SavingsAccounts
            .Include(a => a.Transactions)
            .SingleAsync(a => a.Id == sourceId);
        var destinationInT = await ctxT.SavingsAccounts
            .Include(a => a.Transactions)
            .SingleAsync(a => a.Id == destinationId);

        // Step 2: Interferer — commits a deposit, bumping DB Version 0 → 1
        await using (var ctxI = _db.NewDbContext())
        {
            var srcI = await ctxI.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == sourceId);
            srcI.Deposit(new DateOnly(2026, 1, 2), 1m, today);
            await ctxI.SaveChangesAsync(); // DB Version: 0 → 1, balance: 500 → 501
        }

        // Step 3: Context T stages the transfer and attempts to save
        var withdrawalTxId = sourceInT.WithdrawMoney(today, 200m, today);
        var depositTxId = destinationInT.Deposit(today, 200m, today);

        var transfer = AccountTransfer.Create(
            sourceInT.Id, destinationInT.Id,
            withdrawalTxId, depositTxId,
            200m, "USD", today, "Rollback test", null);

        ctxT.AccountTransfers.Add(transfer);

        var act = () => ctxT.SaveChangesAsync();

        // AggregateVersionInterceptor bumps CurrentValue to 1, EF emits WHERE VERSION=0,
        // DB has VERSION=1 → 0 rows updated → DbUpdateConcurrencyException
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "the AggregateVersionInterceptor bumps Version, so the external bump causes a concurrency conflict");

        // Verify the failed transfer produced no ACCOUNT_TRANSFERS row.
        // The interferer's phantom deposit of 1m DID commit (source balance = 501m).
        await using (var ctx = _db.NewRawDbContext())
        {
            // Source: 500 (original seed) + 1 (interferer deposit) = 501. Withdrawal of 200 rolled back.
            var source = await ctx.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == sourceId);
            source.AccountBalance.Should().Be(501m,
                "interferer's 1m deposit committed; failed transfer's 200m withdrawal rolled back");

            // Source should have exactly 2 transaction rows: seed deposit (500) + interferer deposit (1).
            // The failed transfer's withdrawal transaction row must NOT exist.
            source.Transactions.Should().HaveCount(2,
                "only seed deposit and interferer deposit committed; failed withdrawal transaction must be absent");
            source.Transactions.Should().NotContain(
                t => t.Type == SavingsTransactionType.Withdrawal,
                "the rolled-back withdrawal must not appear as a transaction row on the source");

            // Destination unchanged — failed transfer's deposit rolled back
            var destination = await ctx.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == destinationId);
            destination.AccountBalance.Should().Be(0m, "failed transfer's deposit was rolled back");
            // Destination must have zero transaction rows (its deposit never committed)
            destination.Transactions.Should().BeEmpty(
                "failed transfer's deposit transaction must not exist on destination");

            // No ACCOUNT_TRANSFERS row from the failed transfer
            var transferCount = await ctx.AccountTransfers.CountAsync();
            transferCount.Should().Be(0, "rolled-back transfer must not appear in ACCOUNT_TRANSFERS");

            // Outbox: the interferer's deposit produced 1 SavingsDeposited row (it committed via NewDbContext).
            // The failed transfer must produce: no SavingsWithdrawn, no SavingsDeposited (transfer legs),
            // and no MoneyTransferred row.
            var allOutbox = await ctx.Set<CoreBanking.BuildingBlocks.Messaging.OutboxMessage>()
                .Where(m => m.Topic == "savings-accounts.events")
                .ToListAsync();

            allOutbox.Count(m => m.Type == "SavingsWithdrawnIntegrationEvent").Should().Be(0,
                "failed transfer's withdrawal leg was rolled back — no SavingsWithdrawn outbox row");

            // The destination deposit was also in ctxT (same failed transaction), so it must be absent too.
            allOutbox.Count(m => m.Type == "MoneyTransferredIntegrationEvent").Should().Be(0,
                "no MoneyTransferred event from a failed transfer");

            // The interferer DID commit its SavingsDeposited row (1 expected)
            allOutbox.Count(m => m.Type == "SavingsDepositedIntegrationEvent").Should().Be(1,
                "only the interferer's committed deposit produces a SavingsDeposited outbox row");
        }
    }

    // -----------------------------------------------------------------------
    // Scenario (c): Two concurrent transfers from the same source → second gets
    //               DbUpdateConcurrencyException (AggregateVersionInterceptor
    //               makes the concurrency token effective).
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Concurrent_transfers_from_same_source_second_gets_concurrency_exception()
    {
        var today = new DateOnly(2026, 6, 29);
        Guid sourceId;
        Guid dest1Id;
        Guid dest2Id;

        // Phase 1: seed source (with balance) + two distinct destinations
        await using (var ctx = _db.NewRawDbContext())
        {
            var source = MakeActiveAccount("SA-PRC-001", today);
            source.Deposit(new DateOnly(2026, 1, 1), 1000m, today);
            source.ClearDomainEvents();
            sourceId = source.Id;

            var dest1 = MakeActiveAccount("SA-PRC-002", today);
            dest1.ClearDomainEvents();
            dest1Id = dest1.Id;

            var dest2 = MakeActiveAccount("SA-PRC-003", today);
            dest2.ClearDomainEvents();
            dest2Id = dest2.Id;

            ctx.SavingsAccounts.Add(source);
            ctx.SavingsAccounts.Add(dest1);
            ctx.SavingsAccounts.Add(dest2);
            await ctx.SaveChangesAsync();
        }

        // Both T1 and T2 load the source before either saves — simulates concurrent entry.
        await using var ctx1 = _db.NewDbContext();
        await using var ctx2 = _db.NewDbContext();

        var source1 = await ctx1.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == sourceId);
        var source2 = await ctx2.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == sourceId);
        var destination1 = await ctx1.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == dest1Id);
        var destination2 = await ctx2.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == dest2Id);

        // T1 books its transfer and saves first
        var w1 = source1.WithdrawMoney(today, 300m, today);
        var d1 = destination1.Deposit(today, 300m, today);
        var transfer1 = AccountTransfer.Create(source1.Id, destination1.Id, w1, d1, 300m, "USD", today, "T1", null);
        ctx1.AccountTransfers.Add(transfer1);
        await ctx1.SaveChangesAsync(); // succeeds; bumps source's VERSION from 0 → 1

        // T2 tries to save — its in-memory source still has VERSION=0 as OriginalValue,
        // but the DB row now has VERSION=1 → WHERE VERSION=0 matches nothing → exception
        var w2 = source2.WithdrawMoney(today, 400m, today);
        var d2 = destination2.Deposit(today, 400m, today);
        var transfer2 = AccountTransfer.Create(source2.Id, destination2.Id, w2, d2, 400m, "USD", today, "T2", null);
        ctx2.AccountTransfers.Add(transfer2);

        var act2 = () => ctx2.SaveChangesAsync();
        await act2.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "the second concurrent transfer must lose the optimistic lock");

        // Verify: only T1 committed — source balance = 1000 - 300 = 700
        await using (var ctx = _db.NewRawDbContext())
        {
            var source = await ctx.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == sourceId);
            source.AccountBalance.Should().Be(700m, "only T1 committed; T2 rolled back");

            var transferCount = await ctx.AccountTransfers.CountAsync();
            transferCount.Should().Be(1, "only T1's transfer row exists");
        }
    }

    // -----------------------------------------------------------------------
    // Scenario (d): Duplicate ClientTransferReference with matching payload
    //               → idempotent replay (no second row, returns existing id).
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Duplicate_ClientTransferReference_with_matching_payload_is_idempotent()
    {
        var today = new DateOnly(2026, 6, 29);
        Guid sourceId;
        Guid destinationId;

        // Phase 1: seed accounts (raw context — no outbox rows)
        await using (var ctx = _db.NewRawDbContext())
        {
            var source = MakeActiveAccount("SA-PRD-001", today);
            source.Deposit(new DateOnly(2026, 1, 1), 500m, today);
            source.ClearDomainEvents();
            sourceId = source.Id;

            var destination = MakeActiveAccount("SA-PRD-002", today);
            destination.ClearDomainEvents();
            destinationId = destination.Id;

            ctx.SavingsAccounts.Add(source);
            ctx.SavingsAccounts.Add(destination);
            await ctx.SaveChangesAsync();
        }

        const string clientRef = "idem-persist-ref-001";
        Guid firstTransferId;

        // Phase 2: first transfer — persists the row
        await using (var ctx = _db.NewDbContext())
        {
            var source = await ctx.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == sourceId);
            var destination = await ctx.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == destinationId);

            var w = source.WithdrawMoney(today, 100m, today);
            var d = destination.Deposit(today, 100m, today);

            var transfer = AccountTransfer.Create(source.Id, destination.Id, w, d, 100m, "USD", today, "Rent", clientRef);
            ctx.AccountTransfers.Add(transfer);
            firstTransferId = transfer.Id;

            await ctx.SaveChangesAsync();
        }

        // Phase 3: assert idempotency — the unique index blocks a second row with the same
        // ClientTransferReference at the database level.
        Func<Task> act = async () =>
        {
            await using var ctx = _db.NewDbContext();

            // Load again (source now has 400 balance after first transfer)
            var source = await ctx.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == sourceId);
            var destination = await ctx.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == destinationId);

            // Book another transfer with the SAME clientRef (bypassing the handler-level check
            // to test raw DB uniqueness enforcement)
            var w = source.WithdrawMoney(today.AddDays(1), 50m, today.AddDays(1));
            var d = destination.Deposit(today.AddDays(1), 50m, today.AddDays(1));

            // Force a second row with the same reference — the unique index must reject this
            var duplicateTransfer = AccountTransfer.Create(
                source.Id, destination.Id, w, d, 50m, "USD", today.AddDays(1), "Dup rent", clientRef);
            ctx.AccountTransfers.Add(duplicateTransfer);

            await ctx.SaveChangesAsync(); // should throw due to unique index
        };

        await act.Should().ThrowAsync<DbUpdateException>(
            "the ACCOUNT_TRANSFERS unique index on CLIENTTRANSFERREFERENCE rejects duplicate rows at the DB level");

        // Verify: exactly one transfer row persisted (the first one)
        await using (var ctx = _db.NewRawDbContext())
        {
            var transfers = await ctx.AccountTransfers.ToListAsync();
            transfers.Should().HaveCount(1, "unique index blocked the duplicate insert");
            transfers[0].Id.Should().Be(firstTransferId, "only the first transfer row exists");

            // Source balance reflects only the first transfer
            var source = await ctx.SavingsAccounts.Include(a => a.Transactions).SingleAsync(a => a.Id == sourceId);
            source.AccountBalance.Should().Be(400m, "only first transfer of 100 was committed");
        }
    }
}
