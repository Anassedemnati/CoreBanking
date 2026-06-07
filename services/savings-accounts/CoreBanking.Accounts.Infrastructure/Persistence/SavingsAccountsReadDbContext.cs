using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class SavingsAccountsReadDbContext(DbContextOptions<SavingsAccountsReadDbContext> options)
    : DbContext(options)
{
    public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
    public DbSet<SavingsAccountTransaction> SavingsTransactions => Set<SavingsAccountTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("SAVINGS");
        modelBuilder.ApplyConfiguration(new SavingsAccountConfiguration());
        modelBuilder.ApplyConfiguration(new SavingsAccountTransactionConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
