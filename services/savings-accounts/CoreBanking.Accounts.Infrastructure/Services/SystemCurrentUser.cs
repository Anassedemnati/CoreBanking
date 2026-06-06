using CoreBanking.BuildingBlocks.Application;

namespace CoreBanking.Accounts.Infrastructure.Services;

public sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => "system";
}
