using CoreBanking.BuildingBlocks.Application;

namespace CoreBanking.Accounts.Infrastructure.Services;

public sealed class UtcDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
