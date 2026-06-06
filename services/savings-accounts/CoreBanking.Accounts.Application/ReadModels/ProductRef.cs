namespace CoreBanking.Accounts.Application.ReadModels;

public sealed class ProductRef
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = default!;
    public string CurrencyCode { get; set; } = default!;
    public int CurrencyDecimalPlaces { get; set; }
    public decimal DefaultRate { get; set; }
    public long EventVersion { get; set; }
}
