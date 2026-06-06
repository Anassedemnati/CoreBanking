using CoreBanking.Products.Application;

namespace CoreBanking.Products.Infrastructure;

public sealed class ProductUnitOfWork(SavingsProductsWriteDbContext db) : IProductUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
