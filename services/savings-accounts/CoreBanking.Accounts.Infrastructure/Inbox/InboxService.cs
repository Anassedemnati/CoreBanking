using CoreBanking.Accounts.Infrastructure.Persistence;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Messaging;

namespace CoreBanking.Accounts.Infrastructure.Inbox;

public sealed class InboxService(SavingsAccountsWriteDbContext db) : IInboxService
{
    public async Task<bool> HasProcessedAsync(Guid eventId, CancellationToken ct = default)
    {
        var msg = await db.Set<InboxMessage>().FindAsync([eventId], ct);
        return msg?.ProcessedOnUtc is not null;
    }

    public async Task MarkProcessedAsync(Guid eventId, string type, CancellationToken ct = default)
    {
        var existing = await db.Set<InboxMessage>().FindAsync([eventId], ct);
        if (existing is not null)
        {
            existing.ProcessedOnUtc = DateTimeOffset.UtcNow;
        }
        else
        {
            db.Set<InboxMessage>().Add(new InboxMessage
            {
                EventId = eventId,
                Type = type,
                ReceivedOnUtc = DateTimeOffset.UtcNow,
                ProcessedOnUtc = DateTimeOffset.UtcNow
            });
        }
    }
}
