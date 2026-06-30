using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Application.Accounts;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Handlers;

/// <summary>
/// Tests for <see cref="TransferBetweenSavingsAccountsHandler"/> and
/// <see cref="GetAccountTransferByIdHandler"/> — all in-memory, no Docker.
/// </summary>
public sealed class TransferBetweenSavingsAccountsHandlerTests
{
    // -------------------------------------------------------------------------
    // Fakes / doubles
    // -------------------------------------------------------------------------

    private sealed class FakeSavingsAccountRepo : ISavingsAccountRepository
    {
        private readonly Dictionary<Guid, SavingsAccount> _store = new();

        public void Add(SavingsAccount account) => _store[account.Id] = account;

        public Task<SavingsAccount?> FindAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_store.GetValueOrDefault(id));

        public void Seed(SavingsAccount account) => _store[account.Id] = account;
    }

    private sealed class FakeTransferRepo : IAccountTransferRepository
    {
        private readonly Dictionary<Guid, AccountTransfer> _byId = new();
        private readonly Dictionary<string, AccountTransfer> _byRef = new();

        public void Add(AccountTransfer transfer)
        {
            _byId[transfer.Id] = transfer;
            if (transfer.ClientTransferReference is not null)
                _byRef[transfer.ClientTransferReference] = transfer;
        }

        public Task<AccountTransfer?> FindByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_byId.GetValueOrDefault(id));

        public Task<AccountTransfer?> FindByClientReferenceAsync(string clientTransferReference, CancellationToken ct = default)
            => Task.FromResult(_byRef.GetValueOrDefault(clientTransferReference));
    }

    private sealed class FakeUow : ISavingsAccountUnitOfWork
    {
        public int Saves;
        public Task SaveChangesAsync(CancellationToken ct = default) { Saves++; return Task.CompletedTask; }
    }

    /// <summary>Clock pinned after all transfer dates in these tests (2026-06-29).</summary>
    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);
    }

    // -------------------------------------------------------------------------
    // Helpers — build active accounts
    // -------------------------------------------------------------------------

    private static readonly DateOnly ActivationDate = new(2026, 1, 1);
    private static readonly DateOnly TransferDate = new(2026, 6, 29);
    private static readonly DateOnly ClockToday = new(2026, 7, 1);

    private static SavingsAccount MakeActiveAccount(
        string accountNo = "SA-001",
        string currency = "USD",
        int decimalPlaces = 2,
        DateOnly? activationDate = null)
    {
        var on = activationDate ?? ActivationDate;
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), accountNo, currency, decimalPlaces, 5.0m, on);
        a.Approve(on);
        a.Activate(on);
        return a;
    }

    private static TransferBetweenSavingsAccountsHandler BuildHandler(
        FakeSavingsAccountRepo repo,
        FakeTransferRepo transferRepo,
        FakeUow uow)
        => new(repo, transferRepo, uow, new FixedClock());

    // -------------------------------------------------------------------------
    // Happy-path tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Happy_path_books_withdrawal_then_deposit_and_returns_transfer_id()
    {
        var source = MakeActiveAccount("SA-001");
        source.Deposit(ActivationDate, 500m, ClockToday);

        var destination = MakeActiveAccount("SA-002");

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var transferRepo = new FakeTransferRepo();
        var uow = new FakeUow();

        var handler = BuildHandler(repo, transferRepo, uow);
        var cmd = new TransferBetweenSavingsAccountsCommand(
            source.Id, destination.Id, TransferDate, 200m, "Rent");

        var transferId = await handler.Handle(cmd, CancellationToken.None);

        transferId.Should().NotBe(Guid.Empty);
        source.AccountBalance.Should().Be(300m, "500 - 200 = 300");
        destination.AccountBalance.Should().Be(200m, "received 200");
        uow.Saves.Should().Be(1);

        var added = await transferRepo.FindByIdAsync(transferId);
        added.Should().NotBeNull();
        added!.SourceAccountId.Should().Be(source.Id);
        added.DestinationAccountId.Should().Be(destination.Id);
        added.Amount.Should().Be(200m);
    }

    [Fact]
    public async Task Withdrawal_first_insufficiency_throws_before_destination_mutates()
    {
        var source = MakeActiveAccount("SA-001");
        // source has 0 balance — any positive withdrawal fails

        var destination = MakeActiveAccount("SA-002");

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var transferRepo = new FakeTransferRepo();
        var uow = new FakeUow();

        var handler = BuildHandler(repo, transferRepo, uow);
        var cmd = new TransferBetweenSavingsAccountsCommand(
            source.Id, destination.Id, TransferDate, 100m, "Will fail");

        var act = () => handler.Handle(cmd, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Insufficient*");

        destination.AccountBalance.Should().Be(0m, "destination must not have been mutated");
        uow.Saves.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Pre-gate: currency mismatch (both unmutated)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Currency_mismatch_throws_DomainException_before_any_mutation()
    {
        var source = MakeActiveAccount("SA-001", currency: "USD");
        source.Deposit(ActivationDate, 1000m, ClockToday);

        var destination = MakeActiveAccount("SA-002", currency: "EUR");

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var transferRepo = new FakeTransferRepo();
        var uow = new FakeUow();

        var handler = BuildHandler(repo, transferRepo, uow);
        var cmd = new TransferBetweenSavingsAccountsCommand(
            source.Id, destination.Id, TransferDate, 100m, "FX transfer");

        var act = () => handler.Handle(cmd, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.transfer.currency.mismatch");

        source.AccountBalance.Should().Be(1000m, "source must not have been mutated");
        uow.Saves.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Pre-gate: destination not active
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Destination_not_active_throws_DomainException_with_leg_attribution()
    {
        var source = MakeActiveAccount("SA-001");
        source.Deposit(ActivationDate, 1000m, ClockToday);

        // Destination submitted but NOT approved/activated
        var destination = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-002", "USD", 2, 5.0m, ActivationDate);

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var transferRepo = new FakeTransferRepo();
        var uow = new FakeUow();

        var handler = BuildHandler(repo, transferRepo, uow);
        var cmd = new TransferBetweenSavingsAccountsCommand(
            source.Id, destination.Id, TransferDate, 100m, "Inactive dest");

        var act = () => handler.Handle(cmd, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.transfer.destination.notactive");

        source.AccountBalance.Should().Be(1000m, "source must not have been mutated");
        uow.Saves.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Pre-gate: source before pivot
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Source_before_pivot_throws_DomainException_with_source_leg_attribution()
    {
        // source has interest posted through 2026-06-30 — transferDate (2026-06-29) is ON/BEFORE pivot
        var source = MakeActiveAccount("SA-001");
        source.Deposit(new DateOnly(2026, 1, 1), 1000m, ClockToday);
        // Post interest through June so pivot = 2026-06-30
        source.PostInterest(new DateOnly(2026, 6, 30), ClockToday);

        var destination = MakeActiveAccount("SA-002");
        destination.Deposit(new DateOnly(2026, 1, 1), 500m, ClockToday);

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var transferRepo = new FakeTransferRepo();
        var uow = new FakeUow();

        var handler = BuildHandler(repo, transferRepo, uow);
        // transferDate = 2026-06-29 which is BEFORE source pivot (2026-06-30)
        var cmd = new TransferBetweenSavingsAccountsCommand(
            source.Id, destination.Id, new DateOnly(2026, 6, 29), 100m, "Before pivot");

        var act = () => handler.Handle(cmd, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.transfer.source.beforepivot");

        uow.Saves.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Pre-gate: destination before pivot
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Destination_before_pivot_throws_DomainException_with_destination_leg_attribution()
    {
        // source has NO pivot — only destination has a pivot after the transfer date
        var source = MakeActiveAccount("SA-001");
        source.Deposit(new DateOnly(2026, 1, 1), 1000m, ClockToday);
        // source has no interest posted (no pivot)

        var destination = MakeActiveAccount("SA-002");
        destination.Deposit(new DateOnly(2026, 1, 1), 500m, ClockToday);
        // Post interest through June 30 on destination so pivot = 2026-06-30
        destination.PostInterest(new DateOnly(2026, 6, 30), ClockToday);

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var transferRepo = new FakeTransferRepo();
        var uow = new FakeUow();

        var handler = BuildHandler(repo, transferRepo, uow);
        // transferDate = 2026-06-29 which is BEFORE destination pivot (2026-06-30)
        var cmd = new TransferBetweenSavingsAccountsCommand(
            source.Id, destination.Id, new DateOnly(2026, 6, 29), 100m, "Before dest pivot");

        var act = () => handler.Handle(cmd, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.transfer.destination.beforepivot");

        source.AccountBalance.Should().Be(1000m, "source must not have been mutated");
        uow.Saves.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Pre-gate: source not found
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Missing_source_account_throws_NotFoundException()
    {
        var destination = MakeActiveAccount("SA-002");
        var repo = new FakeSavingsAccountRepo();
        repo.Seed(destination);
        var handler = BuildHandler(repo, new FakeTransferRepo(), new FakeUow());

        var act = () => handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                Guid.NewGuid(), destination.Id, TransferDate, 100m, "No source"),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // -------------------------------------------------------------------------
    // Pre-gate: destination not found
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Missing_destination_account_throws_NotFoundException()
    {
        var source = MakeActiveAccount("SA-001");
        source.Deposit(ActivationDate, 1000m, ClockToday);
        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        var handler = BuildHandler(repo, new FakeTransferRepo(), new FakeUow());

        var act = () => handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, Guid.NewGuid(), TransferDate, 100m, "No dest"),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // -------------------------------------------------------------------------
    // Idempotency: matching payload returns existing id without saving
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Duplicate_reference_with_matching_payload_returns_existing_transfer_id()
    {
        var source = MakeActiveAccount("SA-001");
        source.Deposit(ActivationDate, 1000m, ClockToday);
        var destination = MakeActiveAccount("SA-002");

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var transferRepo = new FakeTransferRepo();
        var uow = new FakeUow();
        var handler = BuildHandler(repo, transferRepo, uow);

        const string clientRef = "idempotent-ref-001";

        // First call — creates the transfer
        var firstId = await handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, TransferDate, 100m, "Rent", clientRef),
            CancellationToken.None);

        var savesAfterFirst = uow.Saves;

        // Second call — same payload, same reference
        var secondId = await handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, TransferDate, 100m, "Rent", clientRef),
            CancellationToken.None);

        secondId.Should().Be(firstId, "idempotent replay must return the same transfer id");
        uow.Saves.Should().Be(savesAfterFirst, "no additional save for idempotent replay");
        source.AccountBalance.Should().Be(900m, "only one debit, not two");
    }

    // -------------------------------------------------------------------------
    // Idempotency: conflicting payload throws 422
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Duplicate_reference_with_different_payload_throws_idempotency_conflict()
    {
        var source = MakeActiveAccount("SA-001");
        source.Deposit(ActivationDate, 1000m, ClockToday);
        var destination = MakeActiveAccount("SA-002");

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var transferRepo = new FakeTransferRepo();
        var uow = new FakeUow();
        var handler = BuildHandler(repo, transferRepo, uow);

        const string clientRef = "conflict-ref-001";

        // First call — creates the transfer for 100
        await handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, TransferDate, 100m, "Rent", clientRef),
            CancellationToken.None);

        // Second call — same reference but DIFFERENT amount
        var act = () => handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, TransferDate, 999m, "Different amount", clientRef),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.transfer.idempotency.conflict");
    }

    // -------------------------------------------------------------------------
    // Pre-gate: source not active (status check with leg attribution)
    // Plan §2.4: source not-active must produce "account.transfer.source.notactive"
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Source_not_active_throws_DomainException_with_source_leg_attribution()
    {
        // Source submitted but NOT approved/activated
        var source = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-001", "USD", 2, 5.0m, ActivationDate);

        var destination = MakeActiveAccount("SA-002");
        destination.Deposit(ActivationDate, 1000m, ClockToday);

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var uow = new FakeUow();
        var handler = BuildHandler(repo, new FakeTransferRepo(), uow);

        var act = () => handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, TransferDate, 100m, "Source inactive"),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.transfer.source.notactive");

        uow.Saves.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Pre-gate: source before activation date
    // Plan §2.4: before-activation must produce "account.transfer.source.beforeactivation"
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Source_before_activation_throws_DomainException_with_source_leg_attribution()
    {
        // Activate source on 2026-06-01; transfer date = 2026-01-01 (before activation)
        var lateActivation = new DateOnly(2026, 6, 1);
        var source = MakeActiveAccount("SA-001", activationDate: lateActivation);
        source.Deposit(lateActivation, 1000m, ClockToday);

        var destination = MakeActiveAccount("SA-002");
        destination.Deposit(ActivationDate, 500m, ClockToday);

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var uow = new FakeUow();
        var handler = BuildHandler(repo, new FakeTransferRepo(), uow);

        // Transfer date 2026-01-01 is before source's activation date 2026-06-01
        var act = () => handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, new DateOnly(2026, 1, 1), 100m, "Before source activation"),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.transfer.source.beforeactivation");

        uow.Saves.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Pre-gate: destination before activation date
    // Plan §2.4: before-activation must produce "account.transfer.destination.beforeactivation"
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Destination_before_activation_throws_DomainException_with_destination_leg_attribution()
    {
        var source = MakeActiveAccount("SA-001");
        source.Deposit(ActivationDate, 1000m, ClockToday);

        // Activate destination on 2026-06-01; transfer date = 2026-01-01 (before dest activation)
        var lateActivation = new DateOnly(2026, 6, 1);
        var destination = MakeActiveAccount("SA-002", activationDate: lateActivation);
        destination.Deposit(lateActivation, 100m, ClockToday);

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var uow = new FakeUow();
        var handler = BuildHandler(repo, new FakeTransferRepo(), uow);

        // Transfer date 2026-01-01 is before destination's activation date 2026-06-01
        var act = () => handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, new DateOnly(2026, 1, 1), 100m, "Before dest activation"),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.transfer.destination.beforeactivation");

        source.AccountBalance.Should().Be(1000m, "source must not have been mutated");
        uow.Saves.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Pre-gate: amount precision — throws DomainException (→ 422 in API layer)
    //
    // DIVERGENCE NOTE: Plan Part 4 says sub-currency-precision → 400,
    // but the handler throws DomainException which maps to 422 per
    // ExceptionToProblemDetailsHandler. This test asserts the ACTUAL behaviour
    // (422 / DomainException) so it passes. The divergence is reported in the
    // QA findings — fixing it is migration-dev's job.
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Amount_with_too_many_decimal_places_throws_DomainException_code_amount_precision()
    {
        // Source currency has 2 decimal places; 100.001 has 3 → precision violation
        var source = MakeActiveAccount("SA-001", decimalPlaces: 2);
        source.Deposit(ActivationDate, 1000m, ClockToday);

        var destination = MakeActiveAccount("SA-002");

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var uow = new FakeUow();
        var handler = BuildHandler(repo, new FakeTransferRepo(), uow);

        // 100.001 has 3 decimal places — exceeds currency's 2 decimal places
        var act = () => handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, TransferDate, 100.001m, "Precision test"),
            CancellationToken.None).AsTask();

        // Handler throws DomainException ("account.transfer.amount.precision") → 422.
        // Plan Part 4 specifies 400 for this case — DIVERGENCE (see QA notes).
        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.transfer.amount.precision");

        uow.Saves.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // Fineract parity: balance 100, transfer 95 → source=5, dest=95 (allowed)
    // Fineract parity: balance 100, transfer 100 → source=0, dest=100 (allowed)
    // Fineract parity: balance 100, transfer 100.01 → insufficient balance (rejected)
    // Plan §1.5 worked examples
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Fineract_example_transfer_95_leaves_5_in_source()
    {
        var source = MakeActiveAccount("SA-001");
        source.Deposit(ActivationDate, 100m, ClockToday);

        var destination = MakeActiveAccount("SA-002");

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var uow = new FakeUow();
        var handler = BuildHandler(repo, new FakeTransferRepo(), uow);

        await handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, TransferDate, 95m, "Rent"),
            CancellationToken.None);

        source.AccountBalance.Should().Be(5m, "100 - 95 = 5 (non-negative; v1 has no min-balance)");
        destination.AccountBalance.Should().Be(95m);
    }

    [Fact]
    public async Task Fineract_example_transfer_exact_balance_100_leaves_zero_in_source()
    {
        var source = MakeActiveAccount("SA-001");
        source.Deposit(ActivationDate, 100m, ClockToday);

        var destination = MakeActiveAccount("SA-002");

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var uow = new FakeUow();
        var handler = BuildHandler(repo, new FakeTransferRepo(), uow);

        await handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, TransferDate, 100m, "Full balance"),
            CancellationToken.None);

        source.AccountBalance.Should().Be(0m, "entire balance transferred; v1 has no min-balance");
        destination.AccountBalance.Should().Be(100m);
    }

    [Fact]
    public async Task Fineract_example_transfer_100_01_exceeds_balance_and_is_rejected()
    {
        var source = MakeActiveAccount("SA-001");
        source.Deposit(ActivationDate, 100m, ClockToday);

        var destination = MakeActiveAccount("SA-002");

        var repo = new FakeSavingsAccountRepo();
        repo.Seed(source);
        repo.Seed(destination);
        var uow = new FakeUow();
        var handler = BuildHandler(repo, new FakeTransferRepo(), uow);

        var act = () => handler.Handle(
            new TransferBetweenSavingsAccountsCommand(
                source.Id, destination.Id, TransferDate, 100.01m, "Exceeds balance"),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == "account.balance.insufficient",
                "timeline walk goes negative — Fineract §1.5 worked example");

        source.AccountBalance.Should().Be(100m, "source must not be mutated on failure");
        destination.AccountBalance.Should().Be(0m, "destination must not be mutated");
        uow.Saves.Should().Be(0);
    }
}

// =============================================================================
// GetAccountTransferById handler tests
// =============================================================================

public sealed class GetAccountTransferByIdHandlerTests
{
    [Fact]
    public async Task Handler_returns_dto_when_transfer_exists()
    {
        var transferId = Guid.NewGuid();
        var dto = new AccountTransferDto(
            transferId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            250m,
            "USD",
            new DateOnly(2026, 6, 29),
            "Rent",
            "ref-001",
            DateTimeOffset.UtcNow);

        var readRepo = Substitute.For<ISavingsAccountReadRepository>();
        readRepo.GetAccountTransferAsync(transferId, Arg.Any<CancellationToken>())
            .Returns(dto);

        var handler = new GetAccountTransferByIdHandler(readRepo);
        var result = await handler.Handle(new GetAccountTransferByIdQuery(transferId), CancellationToken.None);

        result.Should().BeEquivalentTo(dto);
    }

    [Fact]
    public async Task Handler_throws_NotFoundException_when_transfer_does_not_exist()
    {
        var readRepo = Substitute.For<ISavingsAccountReadRepository>();
        readRepo.GetAccountTransferAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((AccountTransferDto?)null);

        var handler = new GetAccountTransferByIdHandler(readRepo);

        var act = () => handler.Handle(
            new GetAccountTransferByIdQuery(Guid.NewGuid()),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
