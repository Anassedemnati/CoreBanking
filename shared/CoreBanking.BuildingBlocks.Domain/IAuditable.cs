namespace CoreBanking.BuildingBlocks.Domain;

public interface IAuditable
{
    DateTimeOffset CreatedOnUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? LastModifiedOnUtc { get; set; }
    string? LastModifiedBy { get; set; }
}
