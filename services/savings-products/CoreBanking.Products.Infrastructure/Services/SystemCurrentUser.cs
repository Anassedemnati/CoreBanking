using CoreBanking.BuildingBlocks.Application;

namespace CoreBanking.Products.Infrastructure;

public sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => "system";
}
