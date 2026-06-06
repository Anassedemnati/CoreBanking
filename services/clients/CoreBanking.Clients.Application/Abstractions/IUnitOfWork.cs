namespace CoreBanking.Clients.Application;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}
