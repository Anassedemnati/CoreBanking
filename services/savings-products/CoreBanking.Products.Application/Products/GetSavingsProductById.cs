using Mediator;
using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Products.Application.Products;

public sealed record GetSavingsProductByIdQuery(Guid ProductId) : IQuery<SavingsProductDto>;

public sealed class GetSavingsProductByIdHandler(ISavingsProductReadRepository readRepo)
    : IQueryHandler<GetSavingsProductByIdQuery, SavingsProductDto>
{
    public async ValueTask<SavingsProductDto> Handle(GetSavingsProductByIdQuery query, CancellationToken ct)
    {
        return await readRepo.FindDtoAsync(query.ProductId, ct)
            ?? throw new NotFoundException(nameof(Domain.SavingsProduct), query.ProductId);
    }
}
