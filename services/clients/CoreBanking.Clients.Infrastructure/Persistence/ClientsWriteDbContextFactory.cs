using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreBanking.Clients.Infrastructure;

public sealed class ClientsWriteDbContextFactory : IDesignTimeDbContextFactory<ClientsWriteDbContext>
{
    public ClientsWriteDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ClientsWriteDbContext>()
            .UseOracle("User Id=CLIENTS;Password=dev;Data Source=localhost:1521/FREEPDB1")
            .Options;
        return new ClientsWriteDbContext(options);
    }
}
