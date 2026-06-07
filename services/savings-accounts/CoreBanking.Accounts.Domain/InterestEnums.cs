namespace CoreBanking.Accounts.Domain;

/// <summary>Ids aligned with Fineract's SavingsCompoundingInterestPeriodType.</summary>
public enum InterestCompoundingPeriod
{
    Daily = 1,
    Monthly = 4
}

/// <summary>Ids aligned with Fineract's SavingsPostingInterestPeriodType.</summary>
public enum InterestPostingPeriod
{
    Monthly = 4,
    Quarterly = 5,
    BiAnnual = 6,
    Annual = 7
}

/// <summary>Ids aligned with Fineract's SavingsInterestCalculationDaysInYearType.</summary>
public enum DaysInYearType
{
    Days360 = 360,
    Days365 = 365
}
