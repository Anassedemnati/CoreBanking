using CoreBanking.Clients.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Clients.Infrastructure;

public sealed class ClientsReadDbContext(DbContextOptions<ClientsReadDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("CLIENTS");
        modelBuilder.ApplyConfiguration(new ClientConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}
