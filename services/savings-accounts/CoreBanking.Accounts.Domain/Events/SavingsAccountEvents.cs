using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Accounts.Domain.Events;

public sealed record SavingsAccountSubmitted(Guid AccountId, Guid ClientId, Guid ProductId) : IDomainEvent;
public sealed record SavingsAccountApproved(Guid AccountId, DateOnly ApprovedOn) : IDomainEvent;
public sealed record SavingsAccountActivated(Guid AccountId, DateOnly ActivatedOn) : IDomainEvent;
public sealed record SavingsAccountRejected(Guid AccountId, DateOnly RejectedOn) : IDomainEvent;
public sealed record SavingsAccountWithdrawn(Guid AccountId, DateOnly WithdrawnOn) : IDomainEvent;

public sealed record SavingsDeposited(Guid AccountId, Guid TransactionId, DateOnly On, decimal Amount, decimal BalanceAfter) : IDomainEvent;
public sealed record SavingsWithdrawn(Guid AccountId, Guid TransactionId, DateOnly On, decimal Amount, decimal BalanceAfter) : IDomainEvent;
public sealed record SavingsInterestPosted(Guid AccountId, Guid TransactionId, DateOnly PostedThrough, decimal Amount, decimal BalanceAfter) : IDomainEvent;
