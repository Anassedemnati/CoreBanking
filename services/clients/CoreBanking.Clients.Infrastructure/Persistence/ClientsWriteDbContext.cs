using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Messaging;
using CoreBanking.Clients.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Clients.Infrastructure;

public sealed class ClientsWriteDbContext(DbContextOptions<ClientsWriteDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("CLIENTS");
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
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
