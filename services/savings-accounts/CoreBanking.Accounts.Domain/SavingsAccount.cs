using CoreBanking.Accounts.Domain.Events;
using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Accounts.Domain;

public sealed class SavingsAccount : AggregateRoot, IAuditable
{
    public string AccountNo { get; private set; } = default!;
    public Guid ClientId { get; private set; }
    public Guid ProductId { get; private set; }
    public SavingsAccountStatus Status { get; private set; }
    public string CurrencyCode { get; private set; } = default!;
    public int CurrencyDecimalPlaces { get; private set; }
    public decimal NominalAnnualRate { get; private set; }
    public decimal AccountBalance { get; private set; }
    public InterestCompoundingPeriod Compounding { get; private set; }
    public InterestPostingPeriod PostingPeriod { get; private set; }
    public DaysInYearType DaysInYear { get; private set; }
    public DateOnly? InterestPostedTillDate { get; private set; }

    private readonly List<SavingsAccountTransaction> _transactions = new();
    public IReadOnlyList<SavingsAccountTransaction> Transactions => _transactions.AsReadOnly();

    public DateOnly SubmittedOn { get; private set; }
    public DateOnly? ApprovedOn { get; private set; }
    public DateOnly? ActivatedOn { get; private set; }
    public DateOnly? RejectedOn { get; private set; }
    public DateOnly? WithdrawnOn { get; private set; }

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedOnUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private SavingsAccount(Guid id) : base(id) { }  // EF constructor

    public static SavingsAccount SubmitApplication(
        Guid clientId,
        Guid productId,
        string accountNo,
        string currencyCode,
        int currencyDecimalPlaces,
        decimal nominalAnnualRate,
        DateOnly submittedOn,
        InterestCompoundingPeriod compounding = InterestCompoundingPeriod.Monthly,
        InterestPostingPeriod postingPeriod = InterestPostingPeriod.Monthly,
        DaysInYearType daysInYear = DaysInYearType.Days365)
    {
        if (string.IsNullOrWhiteSpace(accountNo))
            throw new DomainException("account.no.required", "Account number is required.");

        var a = new SavingsAccount(Guid.CreateVersion7())
        {
            ClientId = clientId,
            ProductId = productId,
            AccountNo = accountNo,
            CurrencyCode = currencyCode,
            CurrencyDecimalPlaces = currencyDecimalPlaces,
            NominalAnnualRate = nominalAnnualRate,
            SubmittedOn = submittedOn,
            Status = SavingsAccountStatus.Submitted,
            Compounding = compounding,
            PostingPeriod = postingPeriod,
            DaysInYear = daysInYear,
            AccountBalance = 0m,
        };
        a.Raise(new SavingsAccountSubmitted(a.Id, clientId, productId));
        return a;
    }

    public void Approve(DateOnly on)
    {
        Require(SavingsAccountStatus.Submitted, "approve");
        Status = SavingsAccountStatus.Approved;
        ApprovedOn = on;
        Raise(new SavingsAccountApproved(Id, on));
    }

    public void Activate(DateOnly on)
    {
        Require(SavingsAccountStatus.Approved, "activate");
        Status = SavingsAccountStatus.Active;
        ActivatedOn = on;
        Raise(new SavingsAccountActivated(Id, on));
    }

    public void Reject(DateOnly on)
    {
        Require(SavingsAccountStatus.Submitted, "reject");
        Status = SavingsAccountStatus.Rejected;
        RejectedOn = on;
        Raise(new SavingsAccountRejected(Id, on));
    }

    public void Withdraw(DateOnly on)
    {
        Require(SavingsAccountStatus.Submitted, "withdraw");
        Status = SavingsAccountStatus.Withdrawn;
        WithdrawnOn = on;
        Raise(new SavingsAccountWithdrawn(Id, on));
    }

    public Guid Deposit(DateOnly on, decimal amount, DateOnly today)
    {
        EnsureTransactionAllowed(on, today);
        EnsurePositive(amount);
        var tx = AddTransaction(SavingsTransactionType.Deposit, on, amount);
        Raise(new SavingsDeposited(Id, tx.Id, on, amount, AccountBalance));
        return tx.Id;
    }

    public Guid WithdrawMoney(DateOnly on, decimal amount, DateOnly today)
    {
        EnsureTransactionAllowed(on, today);
        EnsurePositive(amount);

        // Simulate: replay the timeline with the candidate withdrawal inserted (Fineract
        // validateAccountBalanceConstraints) — reject if balance dips below zero at ANY point.
        var candidate = SavingsAccountTransaction.Create(Id, SavingsTransactionType.Withdrawal, on, amount);
        decimal balance = 0m;
        foreach (var t in _transactions.Append(candidate)
                     .OrderBy(x => x.TransactionDate).ThenBy(x => x.Id))
        {
            balance += t.IsCredit ? t.Amount : -t.Amount;
            if (balance < 0m)
                throw new DomainException("account.balance.insufficient",
                    $"Insufficient balance for a withdrawal of {amount} on {on:yyyy-MM-dd}.");
        }

        _transactions.Add(candidate);
        RebuildRunningBalances();
        Raise(new SavingsWithdrawn(Id, candidate.Id, on, amount, AccountBalance));
        return candidate.Id;
    }

    private void EnsureTransactionAllowed(DateOnly on, DateOnly today)
    {
        if (Status != SavingsAccountStatus.Active)
            throw new DomainException("account.transaction.notactive",
                $"Transactions are not allowed on an account in {Status} status.");
        if (on > today)
            throw new DomainException("account.transaction.future",
                "Transaction date cannot be in the future.");
        if (on < ActivatedOn!.Value)
            throw new DomainException("account.transaction.beforeactivation",
                "Transaction date cannot be before the account's activation date.");
        if (InterestPostedTillDate is { } pivot && on <= pivot)
            throw new DomainException("account.transaction.beforepivot",
                $"Transaction date cannot be on or before the interest posting pivot date {pivot:yyyy-MM-dd}.");
    }

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0m)
            throw new DomainException("account.transaction.amount.invalid",
                "Amount must be greater than zero.");
    }

    private SavingsAccountTransaction AddTransaction(SavingsTransactionType type, DateOnly on, decimal amount)
    {
        var tx = SavingsAccountTransaction.Create(Id, type, on, amount);
        _transactions.Add(tx);
        RebuildRunningBalances();
        return tx;
    }

    private void RebuildRunningBalances()
    {
        decimal balance = 0m;
        foreach (var t in _transactions.OrderBy(x => x.TransactionDate).ThenBy(x => x.Id))
        {
            balance += t.IsCredit ? t.Amount : -t.Amount;
            t.RunningBalance = balance;
        }
        AccountBalance = balance;
    }

    private void Require(SavingsAccountStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException(
                $"account.{action}.invalid",
                $"Cannot {action} an account in {Status} status.");
    }
}
