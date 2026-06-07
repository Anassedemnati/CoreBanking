using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.ReadModels;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class ProductRefRepository(SavingsAccountsWriteDbContext db) : IProductRefRepository
{
    public Task<ProductRef?> FindAsync(Guid productId, CancellationToken ct = default)
        => db.Set<ProductRef>().FindAsync([productId], ct).AsTask();

    public async Task UpsertAsync(ProductRef productRef, CancellationToken ct = default)
    {
        var existing = await db.Set<ProductRef>().FindAsync([productRef.ProductId], ct);
        if (existing is null)
        {
            db.Set<ProductRef>().Add(productRef);
        }
        else
        {
            existing.Name = productRef.Name;
            existing.CurrencyCode = productRef.CurrencyCode;
            existing.CurrencyDecimalPlaces = productRef.CurrencyDecimalPlaces;
            existing.DefaultRate = productRef.DefaultRate;
            existing.EventVersion = productRef.EventVersion;
        }
    }
}
