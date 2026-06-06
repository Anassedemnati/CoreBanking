using CoreBanking.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Products.Infrastructure;

public sealed class SavingsProductConfiguration : IEntityTypeConfiguration<SavingsProduct>
{
    public void Configure(EntityTypeBuilder<SavingsProduct> b)
    {
        b.ToTable("SAVINGS_PRODUCTS");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasColumnName("NAME").HasMaxLength(150).IsRequired();
        b.Property(x => x.ShortName).HasColumnName("SHORTNAME").HasMaxLength(50).IsRequired();
        b.Property(x => x.Status).HasColumnName("STATUSENUM").HasConversion<int>();
        b.Property(x => x.Version).HasColumnName("VERSION").IsConcurrencyToken();
        b.Property(x => x.CreatedOnUtc).HasColumnName("CREATEDONUTC");
        b.Property(x => x.CreatedBy).HasColumnName("CREATEDBY").HasMaxLength(100);
        b.Property(x => x.LastModifiedOnUtc).HasColumnName("LASTMODIFIEDONUTC");
        b.Property(x => x.LastModifiedBy).HasColumnName("LASTMODIFIEDBY").HasMaxLength(100);

        b.OwnsOne(x => x.Currency, nav =>
        {
            nav.Property(x => x.Code).HasColumnName("CURRENCYCODE").HasMaxLength(3).IsRequired();
            nav.Property(x => x.DecimalPlaces).HasColumnName("CURRENCYDECIMALPLACES");
        });

        b.OwnsOne(x => x.InterestSettings, nav =>
        {
            nav.Property(x => x.NominalAnnualRate).HasColumnName("NOMINALANNUALRATE").HasColumnType("NUMBER(18,6)");
            nav.Property(x => x.CompoundingPeriod).HasColumnName("COMPOUNDINGPERIOD");
            nav.Property(x => x.PostingPeriod).HasColumnName("POSTINGPERIOD");
            nav.Property(x => x.CalculationType).HasColumnName("CALCULATIONTYPE");
            nav.Property(x => x.DaysInYearType).HasColumnName("DAYSINYEARTYPE");
        });
    }
}
