using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Accounts.Domain.Events;

public sealed record MoneyTransferred(
    Guid TransferId,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    Guid SourceTransactionId,
    Guid DestinationTransactionId,
    decimal Amount,
    string CurrencyCode,
    DateOnly TransferDate,
    string? ClientTransferReference) : IDomainEvent;
