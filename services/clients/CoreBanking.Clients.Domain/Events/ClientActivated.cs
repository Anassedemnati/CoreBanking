using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Clients.Domain;

public sealed record ClientActivated(Guid ClientId, DateOnly ActivationDate) : IDomainEvent;
