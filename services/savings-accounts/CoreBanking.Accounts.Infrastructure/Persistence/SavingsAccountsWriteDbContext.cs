using CoreBanking.Accounts.Application.ReadModels;
using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Infrastructure.Persistence.Configurations;
using CoreBanking.BuildingBlocks.Infrastructure;
using CoreBanking.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class SavingsAccountsWriteDbContext(DbContextOptions<SavingsAccountsWriteDbContext> options)
    : DbContext(options), IOutboxDbContext
{
    public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
    public DbSet<SavingsAccountTransaction> SavingsTransactions => Set<SavingsAccountTransaction>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<ClientRef> ClientRefs => Set<ClientRef>();
    public DbSet<ProductRef> ProductRefs => Set<ProductRef>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("SAVINGS");
        modelBuilder.ApplyConfiguration(new SavingsAccountConfiguration());
        modelBuilder.ApplyConfiguration(new SavingsAccountTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new ClientRefConfiguration());
        modelBuilder.ApplyConfiguration(new ProductRefConfiguration());
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OUTBOX_MESSAGES");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Content).HasColumnType("CLOB");
            e.Property(x => x.Error).HasMaxLength(2000);
        });
        modelBuilder.Entity<InboxMessage>(e =>
        {
            e.ToTable("INBOX_MESSAGES");
            e.HasKey(x => x.EventId);
            e.Property(x => x.EventId).ValueGeneratedNever();
        });
    }
}
