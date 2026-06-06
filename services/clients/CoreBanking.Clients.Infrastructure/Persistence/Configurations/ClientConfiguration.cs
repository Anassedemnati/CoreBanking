using CoreBanking.Clients.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Clients.Infrastructure;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.ToTable("CLIENTS");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.DisplayName).HasColumnName("DISPLAYNAME").HasMaxLength(150).IsRequired();
        b.Property(x => x.ExternalId).HasColumnName("EXTERNALID").HasMaxLength(100);
        b.HasIndex(x => x.ExternalId).IsUnique();
        b.Property(x => x.Status).HasColumnName("STATUSENUM").HasConversion<int>();
        b.Property(x => x.ActivationDate).HasColumnName("ACTIVATIONDATE");
        b.Property(x => x.Version).HasColumnName("VERSION").IsConcurrencyToken();
        b.Property(x => x.CreatedOnUtc).HasColumnName("CREATEDONUTC");
        b.Property(x => x.CreatedBy).HasColumnName("CREATEDBY").HasMaxLength(100);
        b.Property(x => x.LastModifiedOnUtc).HasColumnName("LASTMODIFIEDONUTC");
        b.Property(x => x.LastModifiedBy).HasColumnName("LASTMODIFIEDBY").HasMaxLength(100);
    }
}
