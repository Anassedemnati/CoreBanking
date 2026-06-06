using CoreBanking.BuildingBlocks.Application;

namespace CoreBanking.Products.Infrastructure;

public sealed class UtcDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
