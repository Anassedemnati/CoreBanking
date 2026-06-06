using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Products.Domain;

public sealed class SavingsProduct : AggregateRoot, IAuditable
{
    public string Name { get; private set; } = default!;
    public string ShortName { get; private set; } = default!;
    public Currency Currency { get; private set; } = default!;
    public InterestSettings InterestSettings { get; private set; } = default!;
    public SavingsProductStatus Status { get; private set; }

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private SavingsProduct(Guid id) : base(id) { }  // EF constructor

    public static SavingsProduct Create(
        string name,
        string shortName,
        Currency currency,
        InterestSettings interestSettings)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("product.name.required", "Name is required.");
        if (string.IsNullOrWhiteSpace(shortName))
            throw new DomainException("product.shortname.required", "ShortName is required.");

        var p = new SavingsProduct(Guid.CreateVersion7())
        {
            Name = name,
            ShortName = shortName,
            Currency = currency,
            InterestSettings = interestSettings,
            Status = SavingsProductStatus.Active
        };
        p.Raise(new SavingsProductCreated(
            p.Id, name, currency.Code, currency.DecimalPlaces,
            interestSettings.NominalAnnualRate));
        return p;
    }
}
