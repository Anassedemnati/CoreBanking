using CoreBanking.Accounts.Application.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Accounts.Infrastructure.Persistence.Configurations;

public sealed class ClientRefConfiguration : IEntityTypeConfiguration<ClientRef>
{
    public void Configure(EntityTypeBuilder<ClientRef> b)
    {
        b.ToTable("CLIENT_REF");
        b.HasKey(x => x.ClientId);
        b.Property(x => x.ClientId).ValueGeneratedNever();
        b.Property(x => x.DisplayName).HasColumnName("DISPLAYNAME").HasMaxLength(150).IsRequired();
        b.Property(x => x.IsActive).HasColumnName("ISACTIVE");
        b.Property(x => x.EventVersion).HasColumnName("EVENTVERSION");
    }
}
