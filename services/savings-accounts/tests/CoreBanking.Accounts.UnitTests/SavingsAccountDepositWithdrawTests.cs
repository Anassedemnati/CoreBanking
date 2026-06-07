using CoreBanking.Accounts.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests;

public sealed class SavingsAccountDepositWithdrawTests
{
    [Fact]
    public void Transaction_credit_debit_semantics_follow_fineract_type_ids()
    {
        ((int)SavingsTransactionType.Deposit).Should().Be(1);
        ((int)SavingsTransactionType.Withdrawal).Should().Be(2);
        ((int)SavingsTransactionType.InterestPosting).Should().Be(3);

        var deposit = SavingsAccountTransaction.Create(
            Guid.NewGuid(), SavingsTransactionType.Deposit, new DateOnly(2026, 1, 5), 100m);
        var withdrawal = SavingsAccountTransaction.Create(
            Guid.NewGuid(), SavingsTransactionType.Withdrawal, new DateOnly(2026, 1, 6), 40m);
        var interest = SavingsAccountTransaction.Create(
            Guid.NewGuid(), SavingsTransactionType.InterestPosting, new DateOnly(2026, 1, 31), 1.25m);

        deposit.IsCredit.Should().BeTrue();
        withdrawal.IsCredit.Should().BeFalse();
        interest.IsCredit.Should().BeTrue();
        deposit.Id.Should().NotBe(Guid.Empty);
    }
}
