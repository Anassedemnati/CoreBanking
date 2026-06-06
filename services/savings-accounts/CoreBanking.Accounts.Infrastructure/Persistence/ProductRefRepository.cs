using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.ReadModels;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

/// <summary>
/// Stub implementation — will be populated from Kafka events in Phase 5.
/// </summary>
public sealed class ProductRefRepository : IProductRefRepository
{
    public Task<ProductRef?> FindAsync(Guid productId, CancellationToken ct = default)
        => Task.FromResult<ProductRef?>(null);
}
