using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Messaging;
using CoreBanking.Products.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Products.Infrastructure;

public sealed class SavingsProductsWriteDbContext(DbContextOptions<SavingsProductsWriteDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    public DbSet<SavingsProduct> SavingsProducts => Set<SavingsProduct>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("PRODUCTS");
        modelBuilder.ApplyConfiguration(new SavingsProductConfiguration());
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OUTBOX_MESSAGES");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Content).HasColumnType("CLOB");
            e.Property(x => x.Error).HasMaxLength(2000);
        });
    }
}
