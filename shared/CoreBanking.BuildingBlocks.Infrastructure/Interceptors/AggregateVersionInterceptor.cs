using CoreBanking.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreBanking.BuildingBlocks.Infrastructure;

/// <summary>
/// Increments <see cref="AggregateRoot.Version"/> for every modified aggregate
/// root at save time, making the concurrency token effective on Oracle.
/// </summary>
/// <remarks>
/// EF Core does not auto-increment a plain <c>int</c> concurrency token on Oracle
/// (there is no <c>rowversion</c> / <c>ROWVERSION</c> equivalent).  Without this
/// interceptor every UPDATE emits <c>WHERE VERSION=0</c>, both concurrent updates
/// match, and no <see cref="DbUpdateConcurrencyException"/> ever fires.  This
/// interceptor closes the latent double-spend window that would otherwise allow two
/// concurrent transfers from the same source account to both pass the in-memory
/// insufficiency check and commit.
///
/// Added entities keep <c>Version=0</c>; only <see cref="EntityState.Modified"/>
/// entries are bumped.  The <c>OriginalValue</c> (the value loaded from the DB)
/// stays unchanged so EF emits <c>SET VERSION = orig+1 WHERE VERSION = orig</c>,
/// which is exactly the optimistic-locking pattern.
/// </remarks>
public sealed class AggregateVersionInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is not null)
        {
            foreach (var entry in ctx.ChangeTracker.Entries<AggregateRoot>())
            {
                if (entry.State == EntityState.Modified)
                {
                    var versionProp = entry.Property(nameof(AggregateRoot.Version));
                    versionProp.CurrentValue = (int)versionProp.OriginalValue! + 1;
                }
            }
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
