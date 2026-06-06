using CoreBanking.Products.Domain;

namespace CoreBanking.Products.Application;

public interface ISavingsProductRepository
{
    void Add(SavingsProduct product);
    Task<SavingsProduct?> FindAsync(Guid id, CancellationToken ct = default);
}
