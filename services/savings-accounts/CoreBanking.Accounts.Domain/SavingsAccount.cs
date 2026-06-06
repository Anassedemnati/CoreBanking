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
        DateOnly submittedOn)
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
            Status = SavingsAccountStatus.Submitted
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

    private void Require(SavingsAccountStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException(
                $"account.{action}.invalid",
                $"Cannot {action} an account in {Status} status.");
    }
}
