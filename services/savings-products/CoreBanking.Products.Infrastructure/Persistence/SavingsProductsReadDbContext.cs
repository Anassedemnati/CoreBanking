using CoreBanking.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Products.Infrastructure;

public sealed class SavingsProductsReadDbContext(DbContextOptions<SavingsProductsReadDbContext> options)
    : DbContext(options)
{
    public DbSet<SavingsProduct> SavingsProducts => Set<SavingsProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("PRODUCTS");
        modelBuilder.ApplyConfiguration(new SavingsProductConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
