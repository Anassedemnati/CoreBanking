namespace CoreBanking.BuildingBlocks.Application;

public interface ICurrentUser
{
    string? UserId { get; }
}
