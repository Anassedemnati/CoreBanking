using System.Text.Json;
using CoreBanking.BuildingBlocks.Domain;
using CoreBanking.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreBanking.BuildingBlocks.Infrastructure;

public sealed class ConvertDomainEventsToOutboxInterceptor(
    Func<IDomainEvent, IntegrationEvent?> map)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is not null)
        {
            var roots = ctx.ChangeTracker.Entries<AggregateRoot>()
                .Select(e => e.Entity)
                .ToList();

            foreach (var root in roots)
            {
                foreach (var de in root.DomainEvents)
                {
                    var ie = map(de);
                    if (ie is null) continue;

                    ctx.Set<OutboxMessage>().Add(new OutboxMessage
                    {
                        Id = ie.EventId,
                        Type = ie.GetType().Name,
                        Topic = ie.Topic,
                        AggregateKey = ie.AggregateKey,
                        OccurredOnUtc = ie.OccurredOnUtc,
                        Content = JsonSerializer.Serialize(ie, ie.GetType())
                    });
                }
                root.ClearDomainEvents();
            }
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
