using CoreBanking.Accounts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Accounts.Infrastructure.Persistence.Configurations;

public sealed class AccountTransferConfiguration : IEntityTypeConfiguration<AccountTransfer>
{
    public void Configure(EntityTypeBuilder<AccountTransfer> b)
    {
        b.ToTable("ACCOUNT_TRANSFERS");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.SourceAccountId).HasColumnName("SOURCEACCOUNTID");
        b.Property(x => x.DestinationAccountId).HasColumnName("DESTINATIONACCOUNTID");
        b.Property(x => x.SourceTransactionId).HasColumnName("SOURCETRANSACTIONID");
        b.Property(x => x.DestinationTransactionId).HasColumnName("DESTINATIONTRANSACTIONID");

        b.Property(x => x.Amount).HasColumnName("AMOUNT").HasColumnType("NUMBER(19,6)");
        b.Property(x => x.CurrencyCode).HasColumnName("CURRENCYCODE").HasMaxLength(3).IsRequired();
        b.Property(x => x.TransferDate).HasColumnName("TRANSFERDATE");
        b.Property(x => x.Description).HasColumnName("DESCRIPTION").HasMaxLength(100).IsRequired();
        b.Property(x => x.ClientTransferReference).HasColumnName("CLIENTTRANSFERREFERENCE").HasMaxLength(100);

        b.Property(x => x.Version).HasColumnName("VERSION").IsConcurrencyToken();
        b.Property(x => x.CreatedOnUtc).HasColumnName("CREATEDONUTC");
        b.Property(x => x.CreatedBy).HasColumnName("CREATEDBY").HasMaxLength(100);
        b.Property(x => x.LastModifiedOnUtc).HasColumnName("LASTMODIFIEDONUTC");
        b.Property(x => x.LastModifiedBy).HasColumnName("LASTMODIFIEDBY").HasMaxLength(100);

        // Statement-enrichment join indexes (SourceTransactionId / DestinationTransactionId)
        b.HasIndex(x => x.SourceTransactionId).HasDatabaseName("IX_ACCOUNT_TRANSFERS_SOURCETRANSACTIONID");
        b.HasIndex(x => x.DestinationTransactionId).HasDatabaseName("IX_ACCOUNT_TRANSFERS_DESTINATIONTRANSACTIONID");

        // Idempotency key — Oracle natively ignores NULLs in unique indexes; no filter needed.
        b.HasIndex(x => x.ClientTransferReference)
            .IsUnique()
            .HasFilter(null)
            .HasDatabaseName("IX_ACCOUNT_TRANSFERS_CLIENTTRANSFERREFERENCE");
    }
}
