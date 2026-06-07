using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.Accounts;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Handlers;

public sealed class CloseSavingsAccountHandlerTests
{
    private sealed class FakeRepo : ISavingsAccountRepository
    {
        public SavingsAccount? Account;
        public void Add(SavingsAccount account) => Account = account;
        public Task<SavingsAccount?> FindAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(Account?.Id == id ? Account : null);
    }
    private sealed class FakeUow : ISavingsAccountUnitOfWork
    {
        public int Saves;
        public Task SaveChangesAsync(CancellationToken ct = default) { Saves++; return Task.CompletedTask; }
    }
    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 6, 7, 12, 0, 0, TimeSpan.Zero);
    }

    private static SavingsAccount ActiveWithDeposit()
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-H", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        a.Approve(new DateOnly(2026, 1, 1));
        a.Activate(new DateOnly(2026, 1, 1));
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, new DateOnly(2026, 6, 7));
        return a;
    }

    [Fact]
    public async Task Handler_sweeps_closes_and_saves()
    {
        var account = ActiveWithDeposit();
        var repo = new FakeRepo { Account = account };
        var uow = new FakeUow();
        var handler = new CloseSavingsAccountHandler(repo, uow, new FixedClock());

        await handler.Handle(
            new CloseSavingsAccountCommand(account.Id, new DateOnly(2026, 3, 15), WithdrawBalance: true),
            CancellationToken.None);

        account.Status.Should().Be(SavingsAccountStatus.Closed);
        account.AccountBalance.Should().Be(0m);
        uow.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Handler_throws_NotFound_for_unknown_account()
    {
        var handler = new CloseSavingsAccountHandler(new FakeRepo(), new FakeUow(), new FixedClock());
        var act = () => handler.Handle(
            new CloseSavingsAccountCommand(Guid.NewGuid(), new DateOnly(2026, 3, 15)),
            CancellationToken.None).AsTask();
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
