using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Clients.Domain;

public sealed class Client : AggregateRoot, IAuditable
{
    public string DisplayName { get; private set; } = default!;
    public string? ExternalId { get; private set; }
    public ClientStatus Status { get; private set; }
    public DateOnly? ActivationDate { get; private set; }

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private Client(Guid id) : base(id) { }   // EF constructor

    public static Client Register(string displayName, string? externalId)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("client.name.required", "Display name is required.");

        var client = new Client(Guid.CreateVersion7())
        {
            DisplayName = displayName,
            ExternalId = externalId,
            Status = ClientStatus.Pending
        };
        client.Raise(new ClientRegistered(client.Id, displayName, externalId));
        return client;
    }

    public void Activate(DateOnly on)
    {
        if (Status != ClientStatus.Pending)
            throw new DomainException("client.activate.invalid",
                $"Cannot activate a {Status} client.");

        Status = ClientStatus.Active;
        ActivationDate = on;
        Raise(new ClientActivated(Id, on));
    }
}
