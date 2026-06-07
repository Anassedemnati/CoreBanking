using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Accounts.Domain;

public sealed class SavingsAccountTransaction : Entity
{
    public Guid AccountId { get; private set; }
    public SavingsTransactionType Type { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public decimal Amount { get; private set; }
    // Recomputed by SavingsAccount when the transaction timeline changes.
    public decimal RunningBalance { get; internal set; }

    public bool IsCredit => Type switch
    {
        SavingsTransactionType.Deposit => true,
        SavingsTransactionType.Withdrawal => false,
        SavingsTransactionType.InterestPosting => true,
        _ => throw new InvalidOperationException($"IsCredit not defined for {Type}")
    };

    private SavingsAccountTransaction(Guid id) : base(id) { }  // EF constructor

    internal static SavingsAccountTransaction Create(
        Guid accountId, SavingsTransactionType type, DateOnly transactionDate, decimal amount)
        => new(Guid.CreateVersion7())
        {
            AccountId = accountId,
            Type = type,
            TransactionDate = transactionDate,
            Amount = amount
        };
}
