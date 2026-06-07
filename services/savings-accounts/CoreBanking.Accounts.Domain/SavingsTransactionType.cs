namespace CoreBanking.Accounts.Domain;

/// <summary>Transaction types, ids aligned with Fineract's SavingsAccountTransactionType.</summary>
public enum SavingsTransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    InterestPosting = 3
}
