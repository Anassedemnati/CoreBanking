using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreBanking.Products.Infrastructure;

public sealed class SavingsProductsWriteDbContextFactory : IDesignTimeDbContextFactory<SavingsProductsWriteDbContext>
{
    public SavingsProductsWriteDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SavingsProductsWriteDbContext>()
            .UseOracle("User Id=PRODUCTS;Password=dev;Data Source=localhost:1521/FREEPDB1")
            .Options;
        return new SavingsProductsWriteDbContext(options);
    }
}
