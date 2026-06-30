using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class SavingsAccountsReadDbContext(DbContextOptions<SavingsAccountsReadDbContext> options)
    : DbContext(options)
{
    public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
    public DbSet<SavingsAccountTransaction> SavingsTransactions => Set<SavingsAccountTransaction>();
    public DbSet<AccountTransfer> AccountTransfers => Set<AccountTransfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("SAVINGS");
        modelBuilder.ApplyConfiguration(new SavingsAccountConfiguration());
        modelBuilder.ApplyConfiguration(new SavingsAccountTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new AccountTransferConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
