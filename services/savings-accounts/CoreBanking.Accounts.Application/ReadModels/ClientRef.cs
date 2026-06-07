namespace CoreBanking.Accounts.Application.ReadModels;

public sealed class ClientRef
{
    public Guid ClientId { get; set; }
    public string DisplayName { get; set; } = default!;
    public bool IsActive { get; set; }
    public long EventVersion { get; set; }
}
