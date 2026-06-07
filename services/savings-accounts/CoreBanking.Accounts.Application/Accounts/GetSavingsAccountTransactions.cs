using CoreBanking.Accounts.Application.Abstractions;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record SavingsTransactionDto(
    Guid Id,
    int TypeId,
    string Type,
    DateOnly TransactionDate,
    decimal Amount,
    decimal RunningBalance);

public sealed record GetSavingsAccountTransactionsQuery(Guid AccountId)
    : IQuery<IReadOnlyList<SavingsTransactionDto>>;

public sealed class GetSavingsAccountTransactionsHandler(ISavingsAccountReadRepository readRepo)
    : IQueryHandler<GetSavingsAccountTransactionsQuery, IReadOnlyList<SavingsTransactionDto>>
{
    public async ValueTask<IReadOnlyList<SavingsTransactionDto>> Handle(
        GetSavingsAccountTransactionsQuery query, CancellationToken ct)
    {
        return await readRepo.FindTransactionsAsync(query.AccountId, ct);
    }
}
