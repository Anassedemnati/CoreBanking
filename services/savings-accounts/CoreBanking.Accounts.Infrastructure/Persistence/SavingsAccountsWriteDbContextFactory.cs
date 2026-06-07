using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class SavingsAccountsWriteDbContextFactory : IDesignTimeDbContextFactory<SavingsAccountsWriteDbContext>
{
    public SavingsAccountsWriteDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SavingsAccountsWriteDbContext>()
            .UseOracle("User Id=SAVINGS;Password=dev;Data Source=localhost:1521/FREEPDB1")
            .Options;
        return new SavingsAccountsWriteDbContext(options);
    }
}
