using Mediator;

namespace CoreBanking.Products.Application.Products;

public sealed record ListSavingsProductsQuery() : IQuery<IReadOnlyList<SavingsProductDto>>;

public sealed class ListSavingsProductsHandler(ISavingsProductReadRepository readRepo)
    : IQueryHandler<ListSavingsProductsQuery, IReadOnlyList<SavingsProductDto>>
{
    public async ValueTask<IReadOnlyList<SavingsProductDto>> Handle(
        ListSavingsProductsQuery query, CancellationToken ct)
    {
        return await readRepo.ListAsync(ct);
    }
}
