namespace CoreBanking.Accounts.Application.Accounts;

public sealed record SavingsAccountDto(
    Guid Id,
    string AccountNo,
    Guid ClientId,
    Guid ProductId,
    string Status,
    string CurrencyCode,
    decimal NominalAnnualRate,
    DateOnly SubmittedOn,
    DateOnly? ApprovedOn,
    DateOnly? ActivatedOn,
    DateOnly? RejectedOn,
    DateOnly? WithdrawnOn,
    decimal AccountBalance,
    DateOnly? InterestPostedTillDate,
    DateOnly? ClosedOn);
