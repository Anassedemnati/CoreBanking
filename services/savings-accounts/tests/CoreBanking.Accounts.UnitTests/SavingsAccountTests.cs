using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Events;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests;

public sealed class SavingsAccountTests
{
    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 6);

    private static SavingsAccount MakeSubmitted() =>
        SavingsAccount.SubmitApplication(
            ClientId, ProductId, "SA-0001",
            "USD", 2, 5.0m, Today);

    [Fact]
    public void SubmitApplication_creates_Submitted_account_and_raises_event()
    {
        var account = MakeSubmitted();

        account.Status.Should().Be(SavingsAccountStatus.Submitted);
        account.AccountNo.Should().Be("SA-0001");
        account.ClientId.Should().Be(ClientId);
        account.ProductId.Should().Be(ProductId);
        account.SubmittedOn.Should().Be(Today);
        account.Id.Should().NotBe(Guid.Empty);

        account.DomainEvents.OfType<SavingsAccountSubmitted>().Should().ContainSingle()
            .Which.AccountId.Should().Be(account.Id);
    }

    [Fact]
    public void Approve_from_Submitted_sets_Approved_and_raises_event()
    {
        var account = MakeSubmitted();
        var approveDate = new DateOnly(2026, 6, 7);

        account.Approve(approveDate);

        account.Status.Should().Be(SavingsAccountStatus.Approved);
        account.ApprovedOn.Should().Be(approveDate);
        account.DomainEvents.OfType<SavingsAccountApproved>().Should().ContainSingle()
            .Which.ApprovedOn.Should().Be(approveDate);
    }

    [Fact]
    public void Approve_from_Active_throws_DomainException()
    {
        var account = MakeSubmitted();
        account.Approve(Today);
        account.Activate(Today);

        var act = () => account.Approve(Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.approve.invalid");
    }

    [Fact]
    public void Activate_from_Approved_sets_Active_and_raises_event()
    {
        var account = MakeSubmitted();
        account.Approve(Today);
        var activateDate = new DateOnly(2026, 6, 8);

        account.Activate(activateDate);

        account.Status.Should().Be(SavingsAccountStatus.Active);
        account.ActivatedOn.Should().Be(activateDate);
        account.DomainEvents.OfType<SavingsAccountActivated>().Should().ContainSingle()
            .Which.ActivatedOn.Should().Be(activateDate);
    }

    [Fact]
    public void Activate_from_Submitted_throws_DomainException()
    {
        var account = MakeSubmitted();

        var act = () => account.Activate(Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.activate.invalid");
    }

    [Fact]
    public void Reject_from_Submitted_sets_Rejected()
    {
        var account = MakeSubmitted();
        var rejectDate = new DateOnly(2026, 6, 7);

        account.Reject(rejectDate);

        account.Status.Should().Be(SavingsAccountStatus.Rejected);
        account.RejectedOn.Should().Be(rejectDate);
        account.DomainEvents.OfType<SavingsAccountRejected>().Should().ContainSingle();
    }

    [Fact]
    public void Reject_from_Approved_throws_DomainException()
    {
        var account = MakeSubmitted();
        account.Approve(Today);

        var act = () => account.Reject(Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.reject.invalid");
    }

    [Fact]
    public void Withdraw_from_Submitted_sets_Withdrawn()
    {
        var account = MakeSubmitted();
        var withdrawDate = new DateOnly(2026, 6, 7);

        account.Withdraw(withdrawDate);

        account.Status.Should().Be(SavingsAccountStatus.Withdrawn);
        account.WithdrawnOn.Should().Be(withdrawDate);
        account.DomainEvents.OfType<SavingsAccountWithdrawn>().Should().ContainSingle();
    }

    [Fact]
    public void Withdraw_from_Active_throws_DomainException()
    {
        var account = MakeSubmitted();
        account.Approve(Today);
        account.Activate(Today);

        var act = () => account.Withdraw(Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.withdraw.invalid");
    }
}
