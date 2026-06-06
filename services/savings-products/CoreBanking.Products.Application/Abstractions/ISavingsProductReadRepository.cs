using CoreBanking.Products.Application.Products;

namespace CoreBanking.Products.Application;

public interface ISavingsProductReadRepository
{
    Task<SavingsProductDto?> FindDtoAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SavingsProductDto>> ListAsync(CancellationToken ct = default);
}
