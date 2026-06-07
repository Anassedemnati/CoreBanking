using CoreBanking.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.BuildingBlocks.Infrastructure;

public interface IOutboxDbContext
{
    DbSet<OutboxMessage> Outbox { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
