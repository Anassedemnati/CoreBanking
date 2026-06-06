using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Clients.Domain;

public sealed record ClientRegistered(Guid ClientId, string DisplayName, string? ExternalId) : IDomainEvent;
