namespace CoreBanking.Clients.Application.Clients;

public sealed record ClientDto(
    Guid Id,
    string DisplayName,
    string? ExternalId,
    string Status,
    DateOnly? ActivationDate);
