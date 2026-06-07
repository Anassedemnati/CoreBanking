using CoreBanking.Accounts.Application.ReadModels;

namespace CoreBanking.Accounts.Application.Abstractions;

public interface IProductRefRepository
{
    Task<ProductRef?> FindAsync(Guid productId, CancellationToken ct = default);
    Task UpsertAsync(ProductRef productRef, CancellationToken ct = default);
}
