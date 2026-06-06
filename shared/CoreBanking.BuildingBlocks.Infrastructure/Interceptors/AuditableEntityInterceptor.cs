using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreBanking.BuildingBlocks.Infrastructure;

public sealed class AuditableEntityInterceptor(ICurrentUser user, IDateTimeProvider clock)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is not null)
        {
            foreach (var entry in ctx.ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedOnUtc = clock.UtcNow;
                    entry.Entity.CreatedBy = user.UserId;
                }
                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    entry.Entity.LastModifiedOnUtc = clock.UtcNow;
                    entry.Entity.LastModifiedBy = user.UserId;
                }
            }
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
