using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.BuildingBlocks.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreBanking.Accounts.UnitTests;

/// <summary>
/// Verifies that <see cref="AggregateVersionInterceptor"/> bumps <c>Version</c>
/// for every Modified <see cref="AggregateRoot"/> and leaves Added ones at 0.
/// Uses EF Core InMemory so no Docker / Oracle is required.
/// </summary>
public sealed class AggregateVersionInterceptorTests
{
    // ---------------------------------------------------------------------------
    // Minimal test double — just enough to exercise the interceptor.
    // The interceptor is type-agnostic (operates on AggregateRoot), so a stub
    // aggregate fully proves the behaviour without pulling in Oracle config.
    // ---------------------------------------------------------------------------

    private sealed class StubAggregate(Guid id) : AggregateRoot(id)
    {
        // One mutable property so EF marks the entry Modified after a mutation.
        public string Name { get; set; } = string.Empty;
    }

    private sealed class StubDbContext : DbContext
    {
        public StubDbContext(DbContextOptions<StubDbContext> options) : base(options) { }

        public DbSet<StubAggregate> Stubs => Set<StubAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StubAggregate>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.Name).HasMaxLength(100);
                e.Property(x => x.Version).IsConcurrencyToken();
            });
        }
    }

    private static StubDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<StubDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AggregateVersionInterceptor())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new StubDbContext(options);
    }

    [Fact]
    public async Task Added_entity_keeps_Version_zero()
    {
        await using var ctx = BuildContext();

        var stub = new StubAggregate(Guid.NewGuid()) { Name = "initial" };
        ctx.Stubs.Add(stub);
        await ctx.SaveChangesAsync();

        stub.Version.Should().Be(0);
    }

    [Fact]
    public async Task Modified_entity_Version_increments_by_one_on_each_save()
    {
        await using var ctx = BuildContext();

        var stub = new StubAggregate(Guid.NewGuid()) { Name = "initial" };
        ctx.Stubs.Add(stub);
        await ctx.SaveChangesAsync();

        stub.Version.Should().Be(0, "just inserted — no modification yet");

        stub.Name = "updated";
        await ctx.SaveChangesAsync();

        stub.Version.Should().Be(1, "first modification bumps to 1");

        stub.Name = "updated again";
        await ctx.SaveChangesAsync();

        stub.Version.Should().Be(2, "second modification bumps to 2");
    }

    [Fact]
    public async Task Multiple_roots_each_get_their_own_bump()
    {
        await using var ctx = BuildContext();

        var a = new StubAggregate(Guid.NewGuid()) { Name = "A" };
        var b = new StubAggregate(Guid.NewGuid()) { Name = "B" };
        ctx.Stubs.AddRange(a, b);
        await ctx.SaveChangesAsync();

        a.Name = "A modified";
        b.Name = "B modified";
        await ctx.SaveChangesAsync();

        a.Version.Should().Be(1);
        b.Version.Should().Be(1);
    }

    [Fact]
    public async Task Unmodified_entity_in_same_save_does_not_get_bumped()
    {
        await using var ctx = BuildContext();

        var a = new StubAggregate(Guid.NewGuid()) { Name = "A" };
        var b = new StubAggregate(Guid.NewGuid()) { Name = "B" };
        ctx.Stubs.AddRange(a, b);
        await ctx.SaveChangesAsync();

        // Only mutate 'a'; leave 'b' tracked but untouched.
        a.Name = "A modified";
        // Explicitly tell EF to track 'b' without changes.
        ctx.Stubs.Attach(b);
        await ctx.SaveChangesAsync();

        a.Version.Should().Be(1, "only 'a' was modified");
        b.Version.Should().Be(0, "'b' was not modified — must not be bumped");
    }
}
