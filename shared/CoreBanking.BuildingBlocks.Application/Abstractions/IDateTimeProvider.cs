namespace CoreBanking.BuildingBlocks.Application;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
