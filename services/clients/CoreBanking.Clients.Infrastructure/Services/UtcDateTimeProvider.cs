using CoreBanking.BuildingBlocks.Application;

namespace CoreBanking.Clients.Infrastructure;

public sealed class UtcDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
