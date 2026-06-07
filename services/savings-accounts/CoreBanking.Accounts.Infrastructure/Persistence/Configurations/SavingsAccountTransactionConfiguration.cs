using CoreBanking.Accounts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Accounts.Infrastructure.Persistence.Configurations;

public sealed class SavingsAccountTransactionConfiguration : IEntityTypeConfiguration<SavingsAccountTransaction>
{
    public void Configure(EntityTypeBuilder<SavingsAccountTransaction> b)
    {
        b.ToTable("SAVINGS_ACCOUNT_TRANSACTIONS");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AccountId).HasColumnName("ACCOUNTID");
        b.Property(x => x.Sequence).HasColumnName("SEQUENCENO");
        b.Property(x => x.Type).HasColumnName("TYPEENUM").HasConversion<int>();
        b.Property(x => x.TransactionDate).HasColumnName("TRANSACTIONDATE");
        b.Property(x => x.Amount).HasColumnName("AMOUNT").HasColumnType("NUMBER(19,6)");
        b.Property(x => x.RunningBalance).HasColumnName("RUNNINGBALANCE").HasColumnType("NUMBER(19,6)");
        b.HasIndex(x => new { x.AccountId, x.TransactionDate });
    }
}
