using CoreBanking.Accounts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Accounts.Infrastructure.Persistence.Configurations;

public sealed class SavingsAccountConfiguration : IEntityTypeConfiguration<SavingsAccount>
{
    public void Configure(EntityTypeBuilder<SavingsAccount> b)
    {
        b.ToTable("SAVINGS_ACCOUNTS");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AccountNo).HasColumnName("ACCOUNTNO").HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.AccountNo).IsUnique();
        b.Property(x => x.ClientId).HasColumnName("CLIENTID");
        b.Property(x => x.ProductId).HasColumnName("PRODUCTID");
        b.Property(x => x.Status).HasColumnName("STATUSENUM").HasConversion<int>();
        b.Property(x => x.CurrencyCode).HasColumnName("CURRENCYCODE").HasMaxLength(3).IsRequired();
        b.Property(x => x.CurrencyDecimalPlaces).HasColumnName("CURRENCYDECIMALPLACES");
        b.Property(x => x.NominalAnnualRate).HasColumnName("NOMINALANNUALRATE").HasColumnType("NUMBER(18,6)");
        b.Property(x => x.SubmittedOn).HasColumnName("SUBMITTEDON");
        b.Property(x => x.ApprovedOn).HasColumnName("APPROVEDON");
        b.Property(x => x.ActivatedOn).HasColumnName("ACTIVATEDON");
        b.Property(x => x.RejectedOn).HasColumnName("REJECTEDON");
        b.Property(x => x.WithdrawnOn).HasColumnName("WITHDRAWNON");
        b.Property(x => x.ClosedOn).HasColumnName("CLOSEDON");
        b.Property(x => x.AccountBalance).HasColumnName("ACCOUNTBALANCE").HasColumnType("NUMBER(19,6)");
        b.Property(x => x.Compounding).HasColumnName("COMPOUNDINGENUM").HasConversion<int>();
        b.Property(x => x.PostingPeriod).HasColumnName("POSTINGPERIODENUM").HasConversion<int>();
        b.Property(x => x.DaysInYear).HasColumnName("DAYSINYEARENUM").HasConversion<int>();
        b.Property(x => x.InterestPostedTillDate).HasColumnName("INTERESTPOSTEDTILLDATE");

        b.HasMany(x => x.Transactions)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        b.Property(x => x.Version).HasColumnName("VERSION").IsConcurrencyToken();
        b.Property(x => x.CreatedOnUtc).HasColumnName("CREATEDONUTC");
        b.Property(x => x.CreatedBy).HasColumnName("CREATEDBY").HasMaxLength(100);
        b.Property(x => x.LastModifiedOnUtc).HasColumnName("LASTMODIFIEDONUTC");
        b.Property(x => x.LastModifiedBy).HasColumnName("LASTMODIFIEDBY").HasMaxLength(100);
    }
}
