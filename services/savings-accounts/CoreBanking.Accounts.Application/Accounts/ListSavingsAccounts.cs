using CoreBanking.Accounts.Application.Abstractions;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record ListSavingsAccountsQuery() : IQuery<IReadOnlyList<SavingsAccountDto>>;

public sealed class ListSavingsAccountsHandler(ISavingsAccountReadRepository readRepo)
    : IQueryHandler<ListSavingsAccountsQuery, IReadOnlyList<SavingsAccountDto>>
{
    public async ValueTask<IReadOnlyList<SavingsAccountDto>> Handle(
        ListSavingsAccountsQuery query, CancellationToken ct)
    {
        return await readRepo.ListAsync(ct);
    }
}
