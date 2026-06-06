using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Domain;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record GetSavingsAccountByIdQuery(Guid AccountId) : IQuery<SavingsAccountDto>;

public sealed class GetSavingsAccountByIdHandler(ISavingsAccountReadRepository readRepo)
    : IQueryHandler<GetSavingsAccountByIdQuery, SavingsAccountDto>
{
    public async ValueTask<SavingsAccountDto> Handle(GetSavingsAccountByIdQuery query, CancellationToken ct)
    {
        return await readRepo.FindDtoAsync(query.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), query.AccountId);
    }
}
