using CoreBanking.Products.Application;
using CoreBanking.Products.Domain;

namespace CoreBanking.Products.Infrastructure;

public sealed class SavingsProductRepository(SavingsProductsWriteDbContext db) : ISavingsProductRepository
{
    public void Add(SavingsProduct product) => db.SavingsProducts.Add(product);

    public async Task<SavingsProduct?> FindAsync(Guid id, CancellationToken ct = default)
        => await db.SavingsProducts.FindAsync(new object?[] { id }, ct);
}
