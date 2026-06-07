using CoreBanking.Accounts.Application.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Accounts.Infrastructure.Persistence.Configurations;

public sealed class ProductRefConfiguration : IEntityTypeConfiguration<ProductRef>
{
    public void Configure(EntityTypeBuilder<ProductRef> b)
    {
        b.ToTable("PRODUCT_REF");
        b.HasKey(x => x.ProductId);
        b.Property(x => x.ProductId).ValueGeneratedNever();
        b.Property(x => x.Name).HasColumnName("NAME").HasMaxLength(150).IsRequired();
        b.Property(x => x.CurrencyCode).HasColumnName("CURRENCYCODE").HasMaxLength(3).IsRequired();
        b.Property(x => x.CurrencyDecimalPlaces).HasColumnName("CURRENCYDECIMALPLACES");
        b.Property(x => x.DefaultRate).HasColumnName("DEFAULTRATE").HasColumnType("NUMBER(18,6)");
        b.Property(x => x.EventVersion).HasColumnName("EVENTVERSION");
    }
}
