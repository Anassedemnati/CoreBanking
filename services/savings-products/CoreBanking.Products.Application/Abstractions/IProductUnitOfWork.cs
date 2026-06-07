namespace CoreBanking.Products.Application;

public interface IProductUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}
