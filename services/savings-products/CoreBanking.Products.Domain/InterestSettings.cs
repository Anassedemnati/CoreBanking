namespace CoreBanking.Products.Domain;

public sealed record InterestSettings(
    decimal NominalAnnualRate,
    int CompoundingPeriod,
    int PostingPeriod,
    int CalculationType,
    int DaysInYearType);
