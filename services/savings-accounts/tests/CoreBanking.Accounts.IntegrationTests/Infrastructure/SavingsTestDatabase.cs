using CoreBanking.Accounts.Infrastructure;
using CoreBanking.Accounts.Infrastructure.Persistence;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Testcontainers.Oracle;

namespace CoreBanking.Accounts.IntegrationTests.Infrastructure;

/// <summary>
/// Provider-switchable database fixture for savings-account integration tests.
///
/// Default (no env var, or COREBANKING_TEST_DB=sqlite): creates a single in-memory
/// SQLite connection that persists for the fixture lifetime; the schema is created once
/// via <c>EnsureCreatedAsync</c>.  All three EF interceptors are wired so concurrency
/// and outbox assertions remain meaningful.
///
/// Opt-in (COREBANKING_TEST_DB=oracle): spins an OracleContainer and runs the full
/// migration sequence, validating the Oracle DDL including every migration added so far.
///
/// Usage pattern (IAsyncLifetime delegation — NOT IClassFixture, so each test method
/// gets a fresh, isolated DB):
/// <code>
///   private readonly SavingsTestDatabase _db = new();
///   public Task InitializeAsync() => _db.InitializeAsync();
///   public Task DisposeAsync() => _db.DisposeAsync();
/// </code>
/// </summary>
public sealed class SavingsTestDatabase : IAsyncLifetime
{
    // -----------------------------------------------------------------------
    // Oracle constants (same image as docker/docker-compose.yml oracle-free service)
    // -----------------------------------------------------------------------
    private const string OraPassword = "TestPassword1";

    // -----------------------------------------------------------------------
    // Provider detection
    // -----------------------------------------------------------------------
    private static readonly bool UseOracle =
        string.Equals(
            Environment.GetEnvironmentVariable("COREBANKING_TEST_DB"),
            "oracle",
            StringComparison.OrdinalIgnoreCase);

    public bool IsOracle => UseOracle;
    public string ProviderName => UseOracle ? "Oracle" : "SQLite";

    // -----------------------------------------------------------------------
    // Provider-specific resources
    // -----------------------------------------------------------------------
    private OracleContainer? _oracleContainer;
    private SqliteConnection? _sqliteConnection;

    // Cached options (built once in InitializeAsync, reused by NewDbContext)
    private DbContextOptions<SavingsAccountsWriteDbContext>? _options;

    // -----------------------------------------------------------------------
    // Minimal test doubles for the interceptors
    // -----------------------------------------------------------------------
    private sealed class TestCurrentUser : ICurrentUser
    {
        public string? UserId => "test-user";
    }

    private sealed class TestClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = new DateTimeOffset(2026, 6, 29, 0, 0, 0, TimeSpan.Zero);
    }

    // -----------------------------------------------------------------------
    // IAsyncLifetime
    // -----------------------------------------------------------------------

    public async Task InitializeAsync()
    {
        var outboxMap = new ConvertDomainEventsToOutboxInterceptor(
            DependencyInjection.DomainEventToIntegrationEventMap);
        var versionInterceptor = new AggregateVersionInterceptor();
        var auditInterceptor = new AuditableEntityInterceptor(new TestCurrentUser(), new TestClock());

        if (UseOracle)
        {
            _oracleContainer = new OracleBuilder()
                .WithImage("gvenzl/oracle-free:latest")
                .WithUsername("SAVINGS")
                .WithPassword(OraPassword)
                .Build();

            await _oracleContainer.StartAsync();

            // Testcontainers.Oracle 3.10 hard-codes SERVICE_NAME=XEPDB1; oracle-free uses FREEPDB1.
            var connStr = _oracleContainer.GetConnectionString().Replace("XEPDB1", "FREEPDB1");

            _options = new DbContextOptionsBuilder<SavingsAccountsWriteDbContext>()
                .UseOracle(connStr)
                .AddInterceptors(auditInterceptor, versionInterceptor, outboxMap)
                .Options;

            // Run the full migration sequence to validate Oracle DDL.
            await using var ctx = new SavingsAccountsWriteDbContext(_options);
            await ctx.Database.MigrateAsync();
        }
        else
        {
            // Open a single SQLite in-memory connection and keep it open for the
            // entire fixture lifetime.  An in-memory SQLite DB is destroyed when its
            // last connection closes, so we must not let the connection drop between
            // NewDbContext() calls.
            _sqliteConnection = new SqliteConnection("DataSource=:memory:");
            await _sqliteConnection.OpenAsync();

            _options = new DbContextOptionsBuilder<SavingsAccountsWriteDbContext>()
                .UseSqlite(_sqliteConnection)
                .AddInterceptors(auditInterceptor, versionInterceptor, outboxMap)
                .Options;

            // Create schema once (bypasses migrations — EnsureCreated is sufficient
            // for the fast SQLite path; Oracle migrations are validated by the oracle lane).
            await using var ctx = new SavingsAccountsWriteDbContext(_options);
            await ctx.Database.EnsureCreatedAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_oracleContainer is not null)
            await _oracleContainer.DisposeAsync();

        if (_sqliteConnection is not null)
        {
            await _sqliteConnection.CloseAsync();
            _sqliteConnection.Dispose();
        }
    }

    // -----------------------------------------------------------------------
    // Factory
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns a fresh <see cref="SavingsAccountsWriteDbContext"/> bound to the
    /// same underlying database (same SQLite connection instance / same Oracle
    /// connection string).  Each call creates a new context (new change-tracker),
    /// so callers can independently track the same row to exercise optimistic
    /// concurrency.
    /// </summary>
    public SavingsAccountsWriteDbContext NewDbContext()
    {
        if (_options is null)
            throw new InvalidOperationException("SavingsTestDatabase has not been initialized. Call InitializeAsync first.");

        return new SavingsAccountsWriteDbContext(_options);
    }

    /// <summary>
    /// Returns a fresh context that has NO interceptors wired — useful for seeding
    /// test data without polluting the OUTBOX_MESSAGES table, mirroring the existing
    /// "raw context" pattern used in AccountTransferPersistenceTests.
    /// </summary>
    public SavingsAccountsWriteDbContext NewRawDbContext()
    {
        if (_options is null)
            throw new InvalidOperationException("SavingsTestDatabase has not been initialized. Call InitializeAsync first.");

        // Build options without interceptors from the same connection/connection-string.
        DbContextOptions<SavingsAccountsWriteDbContext> rawOptions;
        if (UseOracle && _oracleContainer is not null)
        {
            var connStr = _oracleContainer.GetConnectionString().Replace("XEPDB1", "FREEPDB1");
            rawOptions = new DbContextOptionsBuilder<SavingsAccountsWriteDbContext>()
                .UseOracle(connStr)
                .Options;
        }
        else if (_sqliteConnection is not null)
        {
            // Re-use the same open connection so we talk to the same in-memory DB.
            rawOptions = new DbContextOptionsBuilder<SavingsAccountsWriteDbContext>()
                .UseSqlite(_sqliteConnection)
                .Options;
        }
        else
        {
            throw new InvalidOperationException("SavingsTestDatabase has not been initialized.");
        }

        return new SavingsAccountsWriteDbContext(rawOptions);
    }
}
