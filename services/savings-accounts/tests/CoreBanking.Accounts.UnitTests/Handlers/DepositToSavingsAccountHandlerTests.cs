using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.Accounts;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Handlers;

public sealed class DepositToSavingsAccountHandlerTests
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

    [Fact]
    public async Task Deposit_handler_loads_account_deposits_and_saves()
    {
        var account = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        account.Approve(new DateOnly(2026, 1, 1));
        account.Activate(new DateOnly(2026, 1, 1));
        var repo = new FakeRepo { Account = account };
        var uow = new FakeUow();
        var handler = new DepositToSavingsAccountHandler(repo, uow, new FixedClock());

        var txId = await handler.Handle(
            new DepositToSavingsAccountCommand(account.Id, new DateOnly(2026, 2, 1), 500m),
            CancellationToken.None);

        account.AccountBalance.Should().Be(500m);
        txId.Should().NotBe(Guid.Empty);
        uow.Saves.Should().Be(1);
    }

    [Fact]
    public async Task Deposit_handler_throws_NotFound_for_unknown_account()
    {
        var handler = new DepositToSavingsAccountHandler(new FakeRepo(), new FakeUow(), new FixedClock());

        var act = () => handler.Handle(
            new DepositToSavingsAccountCommand(Guid.NewGuid(), new DateOnly(2026, 2, 1), 500m),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
