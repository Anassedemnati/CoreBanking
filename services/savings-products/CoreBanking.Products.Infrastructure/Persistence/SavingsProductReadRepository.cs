using CoreBanking.Products.Application;
using CoreBanking.Products.Application.Products;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Products.Infrastructure;

public sealed class SavingsProductReadRepository(SavingsProductsReadDbContext db) : ISavingsProductReadRepository
{
    public async Task<SavingsProductDto?> FindDtoAsync(Guid id, CancellationToken ct = default)
    {
        return await db.SavingsProducts
            .Where(p => p.Id == id)
            .Select(p => new SavingsProductDto(
                p.Id,
                p.Name,
                p.ShortName,
                p.Currency.Code,
                p.Currency.DecimalPlaces,
                p.InterestSettings.NominalAnnualRate,
                p.Status.ToString()))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<SavingsProductDto>> ListAsync(CancellationToken ct = default)
    {
        return await db.SavingsProducts
            .Select(p => new SavingsProductDto(
                p.Id,
                p.Name,
                p.ShortName,
                p.Currency.Code,
                p.Currency.DecimalPlaces,
                p.InterestSettings.NominalAnnualRate,
                p.Status.ToString()))
            .ToListAsync(ct);
    }
}
