using CoreBanking.BuildingBlocks.Application;

namespace CoreBanking.Clients.Infrastructure;

public sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => "system";
}
