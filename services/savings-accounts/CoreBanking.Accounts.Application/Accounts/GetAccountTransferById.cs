using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Domain;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record AccountTransferDto(
    Guid TransferId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    Guid SourceTransactionId,
    Guid DestinationTransactionId,
    decimal Amount,
    string CurrencyCode,
    DateOnly TransferDate,
    string Description,
    string? ClientTransferReference,
    DateTimeOffset CreatedOnUtc);

public sealed record GetAccountTransferByIdQuery(Guid TransferId) : IQuery<AccountTransferDto>;

public sealed class GetAccountTransferByIdHandler(ISavingsAccountReadRepository readRepo)
    : IQueryHandler<GetAccountTransferByIdQuery, AccountTransferDto>
{
    public async ValueTask<AccountTransferDto> Handle(GetAccountTransferByIdQuery query, CancellationToken ct)
    {
        return await readRepo.GetAccountTransferAsync(query.TransferId, ct)
            ?? throw new NotFoundException(nameof(AccountTransfer), query.TransferId);
    }
}
