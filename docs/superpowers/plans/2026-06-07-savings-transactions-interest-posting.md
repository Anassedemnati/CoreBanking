# Savings Transactions & Interest Posting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port Apache Fineract's savings transactions + interest posting engine into the CoreBanking `savings-accounts` service so an active savings account can hold money (deposits/withdrawals with running balances) and earn interest (calculated and posted per Fineract's rules).

**Architecture:** Extend the existing `SavingsAccount` aggregate with a child `SavingsAccountTransaction` entity and a pure-function interest engine (`PostingPeriodCalculator`, `InterestCalculator`, `InterestEngine`). New behaviors follow the established slice pattern: domain method → domain event → outbox integration event → Kafka `savings-accounts.events`; CQRS commands/queries via Mediator; Oracle persistence via EF Core with a new migration.

**Tech Stack:** .NET 10, EF Core 10 + Oracle, martinothamar/Mediator, FluentValidation, Confluent.Kafka (outbox already wired), xUnit + FluentAssertions, Testcontainers.Oracle.

---

# Part 1 — Functional Specification (ported from Apache Fineract)

Source: `fineract-savings/.../portfolio/savings/domain/SavingsAccount.java` (deposit lines 1115-1196, withdraw 1249-1330, balance validation 1465-1519, postInterest 516-620, calculateInterestUsing 822-920), `fineract-core/.../savings/domain/interest/{PostingPeriod,EndOfDayBalance}.java`, `SavingsAccountTransactionType.java`, `SavingsAccountStatusType.java`.

## 1.1 Transaction types (v1 scope)

| Type | Fineract id | Direction | Affects balance |
|---|---|---|---|
| Deposit | 1 | Credit | Yes |
| Withdrawal | 2 | Debit | Yes |
| InterestPosting | 3 | Credit | Yes |

Fineract also defines fees (4,5,7), dividend (8), accrual (10), transfers (12-15), write-off (16), overdraft interest (17), withhold tax (18), escheat (19), holds (20,21). **All out of scope v1** (see Non-Goals).

## 1.2 Deposit rules (Fineract `deposit()`)

- Account must be in **Active** (300) status → error `account.transaction.notactive`.
- Amount must be > 0 → `account.transaction.amount.invalid`.
- Transaction date must not be in the future → `account.transaction.future`.
- Transaction date must not be before `ActivatedOn` → `account.transaction.beforeactivation`.
- Transaction date must not be on/before the **pivot date** (`InterestPostedTillDate`) → `account.transaction.beforepivot` (Fineract's "transaction before pivot date" rule with relaxing days = 0).
- Backdated deposits **after** the pivot are allowed: the transaction is inserted into the chronological timeline and the running balances of all later transactions are recomputed.

## 1.3 Withdrawal rules (Fineract `withdraw()` + `validateAccountBalanceConstraints()`)

- Same status/amount/date validations as deposits.
- **Balance sufficiency — timeline walk:** after inserting the withdrawal at its transaction date, replay all non-reversed transactions in order (credits add, debits subtract); if the running balance goes **below zero at any point in the timeline**, reject with `account.balance.insufficient` (Fineract `InsufficientAccountBalanceException`). This catches both "balance too low today" and "backdated withdrawal that would have overdrawn the account in the past".
- v1 invariant: balance must never go negative (no overdraft, no min-required-balance — see Non-Goals).

## 1.4 Interest settings (account-level snapshot)

Snapshotted onto the account at submission, defaulted in the API command (same pattern as `NominalAnnualRate`/currency today). Enum values keep Fineract's ids:

| Setting | Values (Fineract id) |
|---|---|
| Compounding period | Daily (1), Monthly (4) |
| Posting period | Monthly (4), Quarterly (5), BiAnnual (6), Annual (7) |
| Days in year | 360, 365 |
| Calculation type | Daily balance only (Fineract id 1) — implicit, not stored |

## 1.5 Interest calculation (Fineract `EndOfDayBalance` + `CompoundingPeriod`)

- `dailyRate = NominalAnnualRate / 100 / daysInYear` (full `decimal` precision).
- Daily balances derive from the transaction timeline: each day's end-of-day balance is the running balance after the last transaction on or before that day.
- **Monthly compounding (simple interest within month):** for each calendar month inside the posting period, `monthInterest = Σ over balance spans (balance + interestAccumulatedInPriorMonths) × dailyRate × days`; accumulated interest compounds into the base at each month boundary.
- **Daily compounding:** iterative per-day `interest += (balance + interestAccumulatedSoFar) × dailyRate` — equivalent to `FV = PV·(1+r)^n` but kept entirely in `decimal` (no `Math.Pow` double round-trip).
- Interest stays **unrounded during calculation**; rounded to `CurrencyDecimalPlaces` with `MidpointRounding.AwayFromZero` only when a posting transaction is created.

## 1.6 Interest posting (Fineract `postInterest()`)

- Posting periods are **calendar-aligned**: monthly → last day of month; quarterly → Mar/Jun/Sep/Dec; biannual → Jun/Dec; annual → Dec.
- First period starts at `ActivatedOn` (or `InterestPostedTillDate + 1 day` on subsequent runs). Only periods whose **end ≤ asOf date** are posted; a partial trailing period accrues and is posted on a later run.
- Periods are posted **sequentially**: period N's `InterestPosting` transaction is added to the timeline (updating the running balance) **before** period N+1 is calculated, so interest compounds across posting periods through the balance itself.
- Each posting creates an `InterestPosting` transaction dated the **period end date** (skipped when rounded amount is 0). `InterestPostedTillDate` (the pivot) advances to the period end either way.
- Re-running `PostInterest` with the same `asOf` is a no-op (no periods left after the pivot) → **idempotent**.

## 1.7 Lifecycle interaction

- Transactions and interest posting allowed **only in Active (300)** status (Fineract `isTransactionsAllowed()`).
- Existing state machine (Submitted→Approved→Active / Rejected / Withdrawn) is unchanged.

## 1.8 Non-Goals (explicit v1 cuts — decisions, not oversights)

1. **No backdated transactions on/before the pivot date.** Fineract supports correcting a posted period by reversing and recomputing interest postings. v1 instead **rejects** any transaction dated ≤ `InterestPostedTillDate`. This is what makes forward-only posting correct (posted periods are immutable) and removes all reversal logic. Correcting a missed deposit in a posted period requires a future "adjust/reverse" feature.
2. No overdraft (`allowOverdraft`/`overdraftLimit`), no min-required-balance, no lock-in period.
3. No charges, fees, withholding tax, dividend payouts, holds/releases, transfers, write-off, escheat, dormancy sub-status.
4. No average-daily-balance calculation type; no anniversary-based posting periods.
5. No accrual accounting and no scheduled/COB interest-posting job — posting is API-triggered (`POST /postinterest`). A scheduler can call the same command later.
6. No product-sourced interest settings via Kafka: settings arrive in the submit command (existing snapshot pattern). Extending `SavingsProductCreatedIntegrationEvent` + `ProductRef` is a separate future change to the products service contract.

---

# Part 2 — Technical Design

## 2.1 File structure

All paths relative to `/Users/mac/Documents/Projects/CoreBanking/services/savings-accounts/`.

**Create — Domain:**
- `CoreBanking.Accounts.Domain/SavingsTransactionType.cs` — enum (Fineract ids)
- `CoreBanking.Accounts.Domain/InterestEnums.cs` — `InterestCompoundingPeriod`, `InterestPostingPeriod`, `DaysInYearType`
- `CoreBanking.Accounts.Domain/SavingsAccountTransaction.cs` — child entity
- `CoreBanking.Accounts.Domain/Interest/PostingPeriodCalculator.cs` — calendar period splitting (pure)
- `CoreBanking.Accounts.Domain/Interest/InterestEngine.cs` — `DailyBalanceSpan` + span builder (pure)
- `CoreBanking.Accounts.Domain/Interest/InterestCalculator.cs` — interest formulas (pure)

**Modify — Domain:**
- `CoreBanking.Accounts.Domain/SavingsAccount.cs` — balance, transactions collection, interest settings, `Deposit`/`WithdrawMoney`/`PostInterest`
- `CoreBanking.Accounts.Domain/Events/SavingsAccountEvents.cs` — 3 new domain events

**Create — Application:**
- `CoreBanking.Accounts.Application/Accounts/DepositToSavingsAccount.cs`
- `CoreBanking.Accounts.Application/Accounts/WithdrawFromSavingsAccount.cs`
- `CoreBanking.Accounts.Application/Accounts/PostInterestToSavingsAccount.cs`
- `CoreBanking.Accounts.Application/Accounts/GetSavingsAccountTransactions.cs`

**Modify — Application:**
- `CoreBanking.Accounts.Application/Accounts/SubmitSavingsApplication.cs` — interest-settings fields
- `CoreBanking.Accounts.Application/Accounts/SavingsAccountDto.cs` — `AccountBalance`, `InterestPostedTillDate`
- `CoreBanking.Accounts.Application/Abstractions/ISavingsAccountReadRepository.cs` — transactions query

**Create — Infrastructure:**
- `CoreBanking.Accounts.Infrastructure/Persistence/Configurations/SavingsAccountTransactionConfiguration.cs`
- `CoreBanking.Accounts.Infrastructure/Persistence/Migrations/<timestamp>_AddSavingsTransactions.cs` (generated)

**Modify — Infrastructure:**
- `Persistence/Configurations/SavingsAccountConfiguration.cs` — new columns + navigation
- `Persistence/SavingsAccountsWriteDbContext.cs`, `Persistence/SavingsAccountsReadDbContext.cs` — DbSet + config
- `Persistence/SavingsAccountRepository.cs` — `Include(Transactions)`
- `Persistence/SavingsAccountReadRepository.cs` — DTO projection + transactions query
- `Events/SavingsAccountIntegrationEvents.cs` — 3 new integration events
- `DependencyInjection.cs` — outbox map entries

**Modify — Api:**
- `CoreBanking.Accounts.Api/Controllers/SavingsAccountsController.cs` — 4 endpoints

**Tests:**
- `tests/CoreBanking.Accounts.UnitTests/SavingsAccountDepositWithdrawTests.cs` (create)
- `tests/CoreBanking.Accounts.UnitTests/Interest/PostingPeriodCalculatorTests.cs` (create)
- `tests/CoreBanking.Accounts.UnitTests/Interest/InterestCalculatorTests.cs` (create)
- `tests/CoreBanking.Accounts.UnitTests/SavingsAccountPostInterestTests.cs` (create)
- `tests/CoreBanking.Accounts.UnitTests/Handlers/DepositToSavingsAccountHandlerTests.cs` (create)
- `tests/CoreBanking.Accounts.UnitTests/SavingsAccountTests.cs` (modify — factory gains optional params, stays compiling)
- `tests/CoreBanking.Accounts.IntegrationTests/SavingsTransactionsPersistenceTests.cs` (create — first integration test in this project)

## 2.2 Data model

```
SAVINGS.SAVINGS_ACCOUNTS            (existing, + columns)
  ACCOUNTBALANCE          NUMBER(19,6)  NOT NULL DEFAULT 0
  COMPOUNDINGENUM         NUMBER(10)    NOT NULL DEFAULT 4   -- Monthly
  POSTINGPERIODENUM       NUMBER(10)    NOT NULL DEFAULT 4   -- Monthly
  DAYSINYEARENUM          NUMBER(10)    NOT NULL DEFAULT 365
  INTERESTPOSTEDTILLDATE  DATE          NULL                  -- pivot

SAVINGS.SAVINGS_ACCOUNT_TRANSACTIONS (new)
  ID               RAW(16)       PK
  ACCOUNTID        RAW(16)       FK → SAVINGS_ACCOUNTS, cascade delete
  TYPEENUM         NUMBER(10)    -- 1 deposit, 2 withdrawal, 3 interest posting
  TRANSACTIONDATE  DATE
  AMOUNT           NUMBER(19,6)
  RUNNINGBALANCE   NUMBER(19,6)
  IX (ACCOUNTID, TRANSACTIONDATE)
```

## 2.3 Key design decisions

1. **Interest settings snapshot from the command** — matches how `NominalAnnualRate`/currency already flow; keeps the change inside this service (no products contract version bump).
2. **Transaction ordering** = `(TransactionDate, Sequence)` where `Sequence` is an explicit per-account insertion counter assigned by the aggregate. *(Corrected during execution: the original `(TransactionDate, Id)` design relied on `Guid.CreateVersion7()` time-ordering, which is NOT monotonic within the same millisecond — same-day transactions created in quick succession would order nondeterministically, making the withdrawal negative-balance check flaky.)* The EF mapping needs a `SEQUENCENO` column (Task 10).
3. **Running balances are stored**, recomputed by full timeline replay on every mutation (accounts have few transactions; correctness over micro-optimization; Fineract does the same walk in `validateAccountBalanceConstraints`).
4. **Pivot date** (`InterestPostedTillDate`) makes posted periods immutable → forward-only posting, no reversals (Non-Goal #1).
5. **All money math in `decimal`**; daily compounding is iterative multiplication (no `Math.Pow`); round only at posting time, `AwayFromZero`.
6. **`today` is a parameter** to domain methods; handlers supply it from `IDateTimeProvider.UtcNow` — domain stays clock-free and testable.

## 2.4 Event flow

```
SavingsAccount.Deposit()           → SavingsDeposited       ┐  ConvertDomainEventsToOutbox   OutboxMessage   Kafka
SavingsAccount.WithdrawMoney()     → SavingsWithdrawn       ├─ Interceptor (DI map) ───────→ (same txn) ───→ savings-accounts.events
SavingsAccount.PostInterest()      → SavingsInterestPosted  ┘
```

---

# Part 3 — Tasks

> Run all commands from `/Users/mac/Documents/Projects/CoreBanking`.
> Unit tests: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests`

### Task 0: Branch

- [ ] **Step 1: Create a feature branch**

```bash
git checkout -b feature/savings-transactions-interest
```

---

### Task 1: Domain — transaction type, interest enums, transaction entity

**Files:**
- Create: `services/savings-accounts/CoreBanking.Accounts.Domain/SavingsTransactionType.cs`
- Create: `services/savings-accounts/CoreBanking.Accounts.Domain/InterestEnums.cs`
- Create: `services/savings-accounts/CoreBanking.Accounts.Domain/SavingsAccountTransaction.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/SavingsAccountDepositWithdrawTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SavingsAccountDepositWithdrawTests"`
Expected: FAIL (compile error: `SavingsTransactionType` not defined)

- [ ] **Step 3: Write the implementation**

`SavingsTransactionType.cs`:
```csharp
namespace CoreBanking.Accounts.Domain;

/// <summary>Transaction types, ids aligned with Fineract's SavingsAccountTransactionType.</summary>
public enum SavingsTransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    InterestPosting = 3
}
```

`InterestEnums.cs`:
```csharp
namespace CoreBanking.Accounts.Domain;

/// <summary>Ids aligned with Fineract's SavingsCompoundingInterestPeriodType.</summary>
public enum InterestCompoundingPeriod
{
    Daily = 1,
    Monthly = 4
}

/// <summary>Ids aligned with Fineract's SavingsPostingInterestPeriodType.</summary>
public enum InterestPostingPeriod
{
    Monthly = 4,
    Quarterly = 5,
    BiAnnual = 6,
    Annual = 7
}

/// <summary>Ids aligned with Fineract's SavingsInterestCalculationDaysInYearType.</summary>
public enum DaysInYearType
{
    Days360 = 360,
    Days365 = 365
}
```

`SavingsAccountTransaction.cs`:
```csharp
using CoreBanking.BuildingBlocks.Domain;

namespace CoreBanking.Accounts.Domain;

public sealed class SavingsAccountTransaction : Entity
{
    public Guid AccountId { get; private set; }
    public SavingsTransactionType Type { get; private set; }
    public DateOnly TransactionDate { get; private set; }
    public decimal Amount { get; private set; }
    public decimal RunningBalance { get; internal set; }

    public bool IsCredit => Type is SavingsTransactionType.Deposit or SavingsTransactionType.InterestPosting;

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
```

The test calls `Create` from the test assembly — add `InternalsVisibleTo` to the Domain project. Check `CoreBanking.Accounts.Domain.csproj`; if there is no existing `InternalsVisibleTo`, add inside an `<ItemGroup>`:

```xml
<ItemGroup>
  <InternalsVisibleTo Include="CoreBanking.Accounts.UnitTests" />
</ItemGroup>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SavingsAccountDepositWithdrawTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): add savings transaction entity and interest enums (Fineract ids)"
```

---

### Task 2: Domain — interest settings snapshot at submission

**Files:**
- Modify: `services/savings-accounts/CoreBanking.Accounts.Domain/SavingsAccount.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Application/Accounts/SubmitSavingsApplication.cs`

- [ ] **Step 1: Write the failing test** (append to `SavingsAccountDepositWithdrawTests.cs`)

```csharp
    [Fact]
    public void SubmitApplication_snapshots_interest_settings_with_defaults()
    {
        var account = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0002", "USD", 2, 5.0m, new DateOnly(2026, 6, 6));

        account.Compounding.Should().Be(InterestCompoundingPeriod.Monthly);
        account.PostingPeriod.Should().Be(InterestPostingPeriod.Monthly);
        account.DaysInYear.Should().Be(DaysInYearType.Days365);
        account.AccountBalance.Should().Be(0m);
        account.InterestPostedTillDate.Should().BeNull();
        account.Transactions.Should().BeEmpty();
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SubmitApplication_snapshots_interest_settings"`
Expected: FAIL (compile error: `Compounding` not defined)

- [ ] **Step 3: Extend the aggregate** — in `SavingsAccount.cs`, add properties after `NominalAnnualRate`:

```csharp
    public decimal AccountBalance { get; private set; }
    public InterestCompoundingPeriod Compounding { get; private set; }
    public InterestPostingPeriod PostingPeriod { get; private set; }
    public DaysInYearType DaysInYear { get; private set; }
    public DateOnly? InterestPostedTillDate { get; private set; }

    private readonly List<SavingsAccountTransaction> _transactions = new();
    public IReadOnlyList<SavingsAccountTransaction> Transactions => _transactions.AsReadOnly();
```

Extend the factory signature with optional trailing parameters and set them in the initializer:

```csharp
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
```

and inside the object initializer (after `Status = SavingsAccountStatus.Submitted`):

```csharp
            Compounding = compounding,
            PostingPeriod = postingPeriod,
            DaysInYear = daysInYear
```

Optional parameters keep `SavingsAccountTests.MakeSubmitted()` and the existing handler compiling unchanged.

- [ ] **Step 4: Thread settings through the command** — in `SubmitSavingsApplication.cs`:

Replace the command record with:

```csharp
public sealed record SubmitSavingsApplicationCommand(
    Guid ClientId,
    Guid ProductId,
    string AccountNo,
    string CurrencyCode,
    int CurrencyDecimalPlaces,
    decimal NominalAnnualRate,
    DateOnly SubmittedOn,
    InterestCompoundingPeriod Compounding = InterestCompoundingPeriod.Monthly,
    InterestPostingPeriod PostingPeriod = InterestPostingPeriod.Monthly,
    DaysInYearType DaysInYear = DaysInYearType.Days365) : ICommand<Guid>;
```

Add validator rules inside `SubmitSavingsApplicationValidator`:

```csharp
        RuleFor(x => x.Compounding).IsInEnum();
        RuleFor(x => x.PostingPeriod).IsInEnum();
        RuleFor(x => x.DaysInYear).IsInEnum();
```

In the handler, pass them to the factory:

```csharp
        var account = SavingsAccount.SubmitApplication(
            cmd.ClientId, cmd.ProductId, cmd.AccountNo,
            cmd.CurrencyCode, cmd.CurrencyDecimalPlaces,
            cmd.NominalAnnualRate, cmd.SubmittedOn,
            cmd.Compounding, cmd.PostingPeriod, cmd.DaysInYear);
```

- [ ] **Step 5: Run the full unit test project (old + new tests)**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests`
Expected: PASS (all existing lifecycle tests still green)

- [ ] **Step 6: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): snapshot interest settings on savings account at submission"
```

---

### Task 3: Domain — Deposit()

**Files:**
- Modify: `services/savings-accounts/CoreBanking.Accounts.Domain/SavingsAccount.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Domain/Events/SavingsAccountEvents.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/SavingsAccountDepositWithdrawTests.cs`

- [ ] **Step 1: Write the failing tests** (append; also add `using CoreBanking.Accounts.Domain.Events;` and `using CoreBanking.BuildingBlocks.Domain;` at top, plus the shared helper)

```csharp
    private static readonly DateOnly Today = new(2026, 6, 7);

    private static SavingsAccount MakeActive(DateOnly? activatedOn = null)
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        a.Approve(new DateOnly(2026, 1, 1));
        a.Activate(activatedOn ?? new DateOnly(2026, 1, 1));
        a.ClearDomainEvents();
        return a;
    }

    [Fact]
    public void Deposit_on_active_account_adds_transaction_updates_balance_and_raises_event()
    {
        var a = MakeActive();

        var txId = a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);

        a.AccountBalance.Should().Be(1000m);
        a.Transactions.Should().ContainSingle(t =>
            t.Id == txId && t.Type == SavingsTransactionType.Deposit &&
            t.Amount == 1000m && t.RunningBalance == 1000m);
        a.DomainEvents.OfType<SavingsDeposited>().Should().ContainSingle()
            .Which.BalanceAfter.Should().Be(1000m);
    }

    [Fact]
    public void Deposit_on_non_active_account_throws()
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));

        var act = () => a.Deposit(new DateOnly(2026, 1, 10), 100m, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.notactive");
    }

    [Fact]
    public void Deposit_with_future_date_throws()
    {
        var a = MakeActive();
        var act = () => a.Deposit(Today.AddDays(1), 100m, Today);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.future");
    }

    [Fact]
    public void Deposit_before_activation_date_throws()
    {
        var a = MakeActive(activatedOn: new DateOnly(2026, 2, 1));
        var act = () => a.Deposit(new DateOnly(2026, 1, 15), 100m, Today);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.beforeactivation");
    }

    [Fact]
    public void Deposit_with_non_positive_amount_throws()
    {
        var a = MakeActive();
        var act = () => a.Deposit(new DateOnly(2026, 1, 10), 0m, Today);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.amount.invalid");
    }

    [Fact]
    public void Backdated_deposit_reorders_timeline_and_recomputes_running_balances()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 3, 10), 500m, Today);
        a.Deposit(new DateOnly(2026, 2, 1), 200m, Today); // backdated

        var ordered = a.Transactions.OrderBy(t => t.TransactionDate).ToList();
        ordered[0].RunningBalance.Should().Be(200m);  // Feb 1
        ordered[1].RunningBalance.Should().Be(700m);  // Mar 10 recomputed
        a.AccountBalance.Should().Be(700m);
    }
```

Note: `DomainException` (verified in `shared/CoreBanking.BuildingBlocks.Domain/Exceptions/DomainException.cs`) is `DomainException(string code, string message)` and exposes `public string Code { get; }` — the `.Which.Code` assertions below compile as written.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SavingsAccountDepositWithdrawTests"`
Expected: FAIL (compile error: `Deposit` not defined)

- [ ] **Step 3: Implement** — add to `SavingsAccountEvents.cs`:

```csharp
public sealed record SavingsDeposited(Guid AccountId, Guid TransactionId, DateOnly On, decimal Amount, decimal BalanceAfter) : IDomainEvent;
public sealed record SavingsWithdrawn(Guid AccountId, Guid TransactionId, DateOnly On, decimal Amount, decimal BalanceAfter) : IDomainEvent;
public sealed record SavingsInterestPosted(Guid AccountId, Guid TransactionId, DateOnly PostedThrough, decimal Amount, decimal BalanceAfter) : IDomainEvent;
```

Add to `SavingsAccount.cs` (after `Withdraw`, before `Require`):

```csharp
    public Guid Deposit(DateOnly on, decimal amount, DateOnly today)
    {
        EnsureTransactionAllowed(on, today);
        EnsurePositive(amount);
        var tx = AddTransaction(SavingsTransactionType.Deposit, on, amount);
        Raise(new SavingsDeposited(Id, tx.Id, on, amount, AccountBalance));
        return tx.Id;
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
```

Add `using System.Linq;` is implicit (.NET 10 ImplicitUsings); no extra usings needed beyond the existing ones.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SavingsAccountDepositWithdrawTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): deposit with timeline running-balance recomputation"
```

---

### Task 4: Domain — WithdrawMoney()

**Files:**
- Modify: `services/savings-accounts/CoreBanking.Accounts.Domain/SavingsAccount.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/SavingsAccountDepositWithdrawTests.cs`

- [ ] **Step 1: Write the failing tests** (append)

```csharp
    [Fact]
    public void WithdrawMoney_with_sufficient_balance_succeeds_and_raises_event()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);

        var txId = a.WithdrawMoney(new DateOnly(2026, 1, 20), 400m, Today);

        a.AccountBalance.Should().Be(600m);
        a.Transactions.Should().Contain(t => t.Id == txId && t.Type == SavingsTransactionType.Withdrawal);
        a.DomainEvents.OfType<SavingsWithdrawn>().Should().ContainSingle()
            .Which.BalanceAfter.Should().Be(600m);
    }

    [Fact]
    public void WithdrawMoney_exceeding_balance_throws_insufficient()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 100m, Today);

        var act = () => a.WithdrawMoney(new DateOnly(2026, 1, 20), 100.01m, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.balance.insufficient");
        a.AccountBalance.Should().Be(100m);
        a.Transactions.Should().HaveCount(1); // failed withdrawal left no trace
    }

    [Fact]
    public void Backdated_withdrawal_that_would_overdraw_past_timeline_throws()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 100m, Today);
        a.Deposit(new DateOnly(2026, 3, 1), 1000m, Today);

        // balance today is 1100, but on Feb 1 it was only 100
        var act = () => a.WithdrawMoney(new DateOnly(2026, 2, 1), 500m, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.balance.insufficient");
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~WithdrawMoney"`
Expected: FAIL (compile error: `WithdrawMoney` not defined)

- [ ] **Step 3: Implement** — add to `SavingsAccount.cs` after `Deposit`:

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SavingsAccountDepositWithdrawTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): withdrawal with full-timeline balance sufficiency check"
```

---

### Task 5: Domain — posting period splitting

**Files:**
- Create: `services/savings-accounts/CoreBanking.Accounts.Domain/Interest/PostingPeriodCalculator.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/Interest/PostingPeriodCalculatorTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Interest;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Interest;

public sealed class PostingPeriodCalculatorTests
{
    [Fact]
    public void Monthly_periods_end_on_last_day_of_each_month()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 1, 15), new DateOnly(2026, 3, 31), InterestPostingPeriod.Monthly);

        periods.Should().Equal(
            (new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 31)),
            (new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)),
            (new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)));
    }

    [Fact]
    public void Partial_trailing_period_is_excluded()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 27), InterestPostingPeriod.Monthly);

        periods.Should().Equal((new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));
    }

    [Fact]
    public void Quarterly_periods_end_on_calendar_quarter_ends()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 2, 10), new DateOnly(2026, 6, 30), InterestPostingPeriod.Quarterly);

        periods.Should().Equal(
            (new DateOnly(2026, 2, 10), new DateOnly(2026, 3, 31)),
            (new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)));
    }

    [Fact]
    public void Annual_period_ends_on_december_31()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 3, 1), new DateOnly(2027, 1, 15), InterestPostingPeriod.Annual);

        periods.Should().Equal((new DateOnly(2026, 3, 1), new DateOnly(2026, 12, 31)));
    }

    [Fact]
    public void No_complete_period_returns_empty()
    {
        var periods = PostingPeriodCalculator.Split(
            new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 20), InterestPostingPeriod.Monthly);

        periods.Should().BeEmpty();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~PostingPeriodCalculatorTests"`
Expected: FAIL (compile error: namespace `Interest` not found)

- [ ] **Step 3: Implement** — `PostingPeriodCalculator.cs`:

```csharp
namespace CoreBanking.Accounts.Domain.Interest;

/// <summary>
/// Splits a date range into calendar-aligned interest posting periods
/// (Fineract SavingsPostingInterestPeriodType, calendar variants only).
/// Only periods that END on or before <paramref name="asOf"/> are returned.
/// </summary>
public static class PostingPeriodCalculator
{
    public static IReadOnlyList<(DateOnly Start, DateOnly End)> Split(
        DateOnly start, DateOnly asOf, InterestPostingPeriod postingPeriod)
    {
        var result = new List<(DateOnly, DateOnly)>();
        var cursor = start;
        while (true)
        {
            var end = PeriodEnd(cursor, postingPeriod);
            if (end > asOf) break;
            result.Add((cursor, end));
            cursor = end.AddDays(1);
        }
        return result;
    }

    private static DateOnly PeriodEnd(DateOnly date, InterestPostingPeriod postingPeriod)
    {
        var endMonth = postingPeriod switch
        {
            InterestPostingPeriod.Monthly => date.Month,
            InterestPostingPeriod.Quarterly => ((date.Month - 1) / 3) * 3 + 3,
            InterestPostingPeriod.BiAnnual => date.Month <= 6 ? 6 : 12,
            InterestPostingPeriod.Annual => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(postingPeriod))
        };
        return new DateOnly(date.Year, endMonth, DateTime.DaysInMonth(date.Year, endMonth));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~PostingPeriodCalculatorTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): calendar posting period calculator"
```

---

### Task 6: Domain — daily balance spans + interest formulas

**Files:**
- Create: `services/savings-accounts/CoreBanking.Accounts.Domain/Interest/InterestEngine.cs`
- Create: `services/savings-accounts/CoreBanking.Accounts.Domain/Interest/InterestCalculator.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/Interest/InterestCalculatorTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Interest;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests.Interest;

public sealed class InterestCalculatorTests
{
    // 5% / 365: dailyRate = 0.05/365

    [Fact]
    public void Simple_interest_single_span_monthly_compounding()
    {
        // 1000 for the 31 days of January: 1000 * (0.05/365) * 31 = 4.2465753...
        var spans = new[] { new DailyBalanceSpan(new DateOnly(2026, 1, 1), 31, 1000m) };

        var interest = InterestCalculator.Calculate(
            spans, 5.0m, DaysInYearType.Days365, InterestCompoundingPeriod.Monthly);

        interest.Should().BeApproximately(4.2466m, 0.0001m);
    }

    [Fact]
    public void Balance_change_mid_period_weights_spans_by_days()
    {
        // Jan 1-10 (10 days) at 1000, Jan 11-31 (21 days) at 1500
        var spans = new[]
        {
            new DailyBalanceSpan(new DateOnly(2026, 1, 1), 10, 1000m),
            new DailyBalanceSpan(new DateOnly(2026, 1, 11), 21, 1500m)
        };

        var interest = InterestCalculator.Calculate(
            spans, 5.0m, DaysInYearType.Days365, InterestCompoundingPeriod.Monthly);

        // 1000*r*10 + 1500*r*21 where r = 0.05/365  →  1.369863 + 4.315068 = 5.684931
        interest.Should().BeApproximately(5.6849m, 0.0001m);
    }

    [Fact]
    public void Monthly_compounding_adds_prior_month_interest_to_base()
    {
        // Quarterly posting period Jan-Mar, balance 1000 throughout, monthly compounding.
        // Jan: 1000*r*31 = 4.246575; Feb base 1004.246575: *r*28 = 3.851918 (approx);
        // Mar base 1008.098...: *r*31 = 4.280975 → total ≈ 12.379...
        var spans = new[] { new DailyBalanceSpan(new DateOnly(2026, 1, 1), 90, 1000m) };

        var interest = InterestCalculator.Calculate(
            spans, 5.0m, DaysInYearType.Days365, InterestCompoundingPeriod.Monthly);

        var r = 5.0m / 100m / 365m;
        var jan = 1000m * r * 31;
        var feb = (1000m + jan) * r * 28;
        var mar = (1000m + jan + feb) * r * 31;
        interest.Should().BeApproximately(jan + feb + mar, 0.000001m);
    }

    [Fact]
    public void Daily_compounding_compounds_each_day()
    {
        // 1000 for 10 days at 5%/365 daily compounding:
        // (1+r)^10 - 1 applied to 1000 ≈ 1.37070
        var spans = new[] { new DailyBalanceSpan(new DateOnly(2026, 1, 1), 10, 1000m) };

        var interest = InterestCalculator.Calculate(
            spans, 5.0m, DaysInYearType.Days365, InterestCompoundingPeriod.Daily);

        interest.Should().BeApproximately(1.3707m, 0.0001m);
        // strictly more than simple interest 1.369863
        interest.Should().BeGreaterThan(1000m * (5.0m / 100m / 365m) * 10);
    }

    [Fact]
    public void Days360_uses_360_day_denominator()
    {
        var spans = new[] { new DailyBalanceSpan(new DateOnly(2026, 1, 1), 30, 1200m) };

        var interest = InterestCalculator.Calculate(
            spans, 6.0m, DaysInYearType.Days360, InterestCompoundingPeriod.Monthly);

        // 1200 * (0.06/360) * 30 = 6.00
        interest.Should().BeApproximately(6.0m, 0.0001m);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~InterestCalculatorTests"`
Expected: FAIL (compile error: `DailyBalanceSpan` not found)

- [ ] **Step 3: Implement** — `InterestEngine.cs`:

```csharp
namespace CoreBanking.Accounts.Domain.Interest;

/// <summary>A run of consecutive days with a constant end-of-day balance.</summary>
public sealed record DailyBalanceSpan(DateOnly From, int Days, decimal Balance);

/// <summary>
/// Builds end-of-day balance spans for a period from a transaction timeline
/// (Fineract EndOfDayBalance semantics: a transaction affects its own day's EOD balance).
/// </summary>
public static class InterestEngine
{
    public static IReadOnlyList<DailyBalanceSpan> BuildSpans(
        IEnumerable<SavingsAccountTransaction> transactions, DateOnly periodStart, DateOnly periodEnd)
    {
        var ordered = transactions
            .Where(t => t.TransactionDate <= periodEnd)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Id)
            .ToList();

        // Opening balance = running balance after the last transaction strictly before the period.
        var opening = 0m;
        foreach (var t in ordered)
            if (t.TransactionDate < periodStart) opening = t.RunningBalance;

        var spans = new List<DailyBalanceSpan>();
        var cursor = periodStart;
        var balance = opening;

        foreach (var group in ordered.Where(t => t.TransactionDate >= periodStart)
                     .GroupBy(t => t.TransactionDate))
        {
            if (group.Key > cursor)
            {
                spans.Add(new DailyBalanceSpan(cursor, group.Key.DayNumber - cursor.DayNumber, balance));
                cursor = group.Key;
            }
            balance = group.Last().RunningBalance; // groups preserve (date, id) source order
        }

        spans.Add(new DailyBalanceSpan(cursor, periodEnd.DayNumber - cursor.DayNumber + 1, balance));
        return spans;
    }
}
```

`InterestCalculator.cs`:

```csharp
namespace CoreBanking.Accounts.Domain.Interest;

/// <summary>
/// Interest formulas ported from Fineract's EndOfDayBalance/CompoundingPeriod.
/// All math stays in decimal; the result is UNROUNDED — round only at posting time.
/// </summary>
public static class InterestCalculator
{
    public static decimal Calculate(
        IReadOnlyList<DailyBalanceSpan> spans,
        decimal nominalAnnualRatePercent,
        DaysInYearType daysInYear,
        InterestCompoundingPeriod compounding)
    {
        var dailyRate = nominalAnnualRatePercent / 100m / (int)daysInYear;

        if (compounding == InterestCompoundingPeriod.Daily)
        {
            // FV = PV·(1+r)^n done iteratively to stay in decimal (no Math.Pow round-trip).
            var accumulated = 0m;
            foreach (var span in spans)
                for (var d = 0; d < span.Days; d++)
                    accumulated += (span.Balance + accumulated) * dailyRate;
            return accumulated;
        }

        // Monthly compounding: simple interest within each calendar month;
        // interest accumulated in prior months joins the base at month boundaries.
        var total = 0m;
        foreach (var month in SplitByCalendarMonth(spans))
        {
            var monthBase = total;
            var monthInterest = 0m;
            foreach (var s in month)
                monthInterest += (s.Balance + monthBase) * dailyRate * s.Days;
            total += monthInterest;
        }
        return total;
    }

    private static IEnumerable<List<DailyBalanceSpan>> SplitByCalendarMonth(
        IReadOnlyList<DailyBalanceSpan> spans)
    {
        var flat = new List<DailyBalanceSpan>();
        foreach (var s in spans)
        {
            var from = s.From;
            var remaining = s.Days;
            while (remaining > 0)
            {
                var monthEnd = new DateOnly(from.Year, from.Month,
                    DateTime.DaysInMonth(from.Year, from.Month));
                var take = Math.Min(remaining, monthEnd.DayNumber - from.DayNumber + 1);
                flat.Add(new DailyBalanceSpan(from, take, s.Balance));
                from = from.AddDays(take);
                remaining -= take;
            }
        }
        return flat.GroupBy(s => (s.From.Year, s.From.Month)).Select(g => g.ToList());
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~InterestCalculatorTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): daily-balance interest engine (monthly + daily compounding)"
```

---

### Task 7: Domain — PostInterest() on the aggregate

**Files:**
- Modify: `services/savings-accounts/CoreBanking.Accounts.Domain/SavingsAccount.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/SavingsAccountPostInterestTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Events;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests;

public sealed class SavingsAccountPostInterestTests
{
    private static readonly DateOnly Today = new(2026, 6, 7);

    private static SavingsAccount MakeActiveWithDeposit()
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        a.Approve(new DateOnly(2026, 1, 1));
        a.Activate(new DateOnly(2026, 1, 1));
        a.Deposit(new DateOnly(2026, 1, 1), 1000m, Today);
        a.ClearDomainEvents();
        return a;
    }

    [Fact]
    public void PostInterest_multi_period_compounds_via_posted_balance()
    {
        // CRITICAL multi-period test: period N's posting must be in the balance
        // before period N+1 is calculated.
        var a = MakeActiveWithDeposit();

        a.PostInterest(new DateOnly(2026, 3, 31), Today);

        var postings = a.Transactions
            .Where(t => t.Type == SavingsTransactionType.InterestPosting)
            .OrderBy(t => t.TransactionDate)
            .ToList();

        postings.Should().HaveCount(3);
        // Jan: 1000 * (0.05/365) * 31 = 4.246575 → 4.25
        postings[0].TransactionDate.Should().Be(new DateOnly(2026, 1, 31));
        postings[0].Amount.Should().Be(4.25m);
        // Feb on 1004.25: * (0.05/365) * 28 = 3.851918 → 3.85
        postings[1].TransactionDate.Should().Be(new DateOnly(2026, 2, 28));
        postings[1].Amount.Should().Be(3.85m);
        // Mar on 1008.10: * (0.05/365) * 31 = 4.280972 → 4.28
        postings[2].TransactionDate.Should().Be(new DateOnly(2026, 3, 31));
        postings[2].Amount.Should().Be(4.28m);

        a.AccountBalance.Should().Be(1012.38m);
        a.InterestPostedTillDate.Should().Be(new DateOnly(2026, 3, 31));
        a.DomainEvents.OfType<SavingsInterestPosted>().Should().HaveCount(3);
    }

    [Fact]
    public void PostInterest_is_idempotent_for_same_asOf()
    {
        var a = MakeActiveWithDeposit();
        a.PostInterest(new DateOnly(2026, 3, 31), Today);
        var balanceAfterFirst = a.AccountBalance;
        a.ClearDomainEvents();

        a.PostInterest(new DateOnly(2026, 3, 31), Today);

        a.AccountBalance.Should().Be(balanceAfterFirst);
        a.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void PostInterest_skips_partial_trailing_period()
    {
        var a = MakeActiveWithDeposit();

        a.PostInterest(new DateOnly(2026, 2, 15), Today);

        a.Transactions.Count(t => t.Type == SavingsTransactionType.InterestPosting).Should().Be(1);
        a.InterestPostedTillDate.Should().Be(new DateOnly(2026, 1, 31));
    }

    [Fact]
    public void PostInterest_on_non_active_account_throws()
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-0001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));

        var act = () => a.PostInterest(new DateOnly(2026, 3, 31), Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.postinterest.notactive");
    }

    [Fact]
    public void Transaction_on_or_before_pivot_is_rejected_after_posting()
    {
        var a = MakeActiveWithDeposit();
        a.PostInterest(new DateOnly(2026, 1, 31), Today);

        var act = () => a.Deposit(new DateOnly(2026, 1, 31), 100m, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.transaction.beforepivot");
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SavingsAccountPostInterestTests"`
Expected: FAIL (compile error: `PostInterest` not defined)

- [ ] **Step 3: Implement** — add to `SavingsAccount.cs` (also add `using CoreBanking.Accounts.Domain.Interest;` at the top):

```csharp
    public void PostInterest(DateOnly asOf, DateOnly today)
    {
        if (Status != SavingsAccountStatus.Active)
            throw new DomainException("account.postinterest.notactive",
                $"Cannot post interest on an account in {Status} status.");
        if (asOf > today)
            throw new DomainException("account.transaction.future",
                "Interest posting date cannot be in the future.");

        var start = InterestPostedTillDate?.AddDays(1) ?? ActivatedOn!.Value;

        // Periods are posted SEQUENTIALLY: each period's posting transaction enters the
        // timeline (and thus the balance) before the next period is calculated, so
        // interest compounds across posting periods through the balance itself.
        foreach (var (periodStart, periodEnd) in
                 PostingPeriodCalculator.Split(start, asOf, PostingPeriod))
        {
            var spans = InterestEngine.BuildSpans(_transactions, periodStart, periodEnd);
            var raw = InterestCalculator.Calculate(spans, NominalAnnualRate, DaysInYear, Compounding);
            var amount = Math.Round(raw, CurrencyDecimalPlaces, MidpointRounding.AwayFromZero);

            if (amount != 0m)
            {
                var tx = AddTransaction(SavingsTransactionType.InterestPosting, periodEnd, amount);
                Raise(new SavingsInterestPosted(Id, tx.Id, periodEnd, amount, AccountBalance));
            }
            InterestPostedTillDate = periodEnd;
        }
    }
```

- [ ] **Step 4: Run the full unit test project**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests`
Expected: PASS (all)

- [ ] **Step 5: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): sequential interest posting with pivot date"
```

---

### Task 8: Application — deposit / withdraw commands

**Files:**
- Create: `services/savings-accounts/CoreBanking.Accounts.Application/Accounts/DepositToSavingsAccount.cs`
- Create: `services/savings-accounts/CoreBanking.Accounts.Application/Accounts/WithdrawFromSavingsAccount.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/Handlers/DepositToSavingsAccountHandlerTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
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
```

Note: `IDateTimeProvider` lives in `CoreBanking.BuildingBlocks.Application/Abstractions/IDateTimeProvider.cs` and exposes `DateTimeOffset UtcNow { get; }`. Check the namespace used by its file and adjust the `using` if it is `CoreBanking.BuildingBlocks.Application.Abstractions`.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~DepositToSavingsAccountHandlerTests"`
Expected: FAIL (compile error: `DepositToSavingsAccountCommand` not defined)

- [ ] **Step 3: Implement** — `DepositToSavingsAccount.cs`:

```csharp
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record DepositToSavingsAccountCommand(
    Guid AccountId,
    DateOnly TransactionDate,
    decimal Amount) : ICommand<Guid>;

public sealed class DepositToSavingsAccountValidator : AbstractValidator<DepositToSavingsAccountCommand>
{
    public DepositToSavingsAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class DepositToSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IDateTimeProvider dateTime)
    : ICommandHandler<DepositToSavingsAccountCommand, Guid>
{
    public async ValueTask<Guid> Handle(DepositToSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);

        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);
        var txId = account.Deposit(cmd.TransactionDate, cmd.Amount, today);

        await uow.SaveChangesAsync(ct);
        return txId;
    }
}
```

`WithdrawFromSavingsAccount.cs` (same shape):

```csharp
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record WithdrawFromSavingsAccountCommand(
    Guid AccountId,
    DateOnly TransactionDate,
    decimal Amount) : ICommand<Guid>;

public sealed class WithdrawFromSavingsAccountValidator : AbstractValidator<WithdrawFromSavingsAccountCommand>
{
    public WithdrawFromSavingsAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class WithdrawFromSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IDateTimeProvider dateTime)
    : ICommandHandler<WithdrawFromSavingsAccountCommand, Guid>
{
    public async ValueTask<Guid> Handle(WithdrawFromSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);

        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);
        var txId = account.WithdrawMoney(cmd.TransactionDate, cmd.Amount, today);

        await uow.SaveChangesAsync(ct);
        return txId;
    }
}
```

If `IDateTimeProvider`'s namespace or the `ICommand`/`ICommandHandler` usings differ, mirror the usings of `SubmitSavingsApplication.cs` in the same folder.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~DepositToSavingsAccountHandlerTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): deposit and withdraw application commands"
```

---

### Task 9: Application — post-interest command, transactions query, DTO update

**Files:**
- Create: `services/savings-accounts/CoreBanking.Accounts.Application/Accounts/PostInterestToSavingsAccount.cs`
- Create: `services/savings-accounts/CoreBanking.Accounts.Application/Accounts/GetSavingsAccountTransactions.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Application/Accounts/SavingsAccountDto.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Application/Abstractions/ISavingsAccountReadRepository.cs`

- [ ] **Step 1: Implement post-interest command** — `PostInterestToSavingsAccount.cs`:

```csharp
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record PostInterestToSavingsAccountCommand(
    Guid AccountId,
    DateOnly AsOf) : ICommand;

public sealed class PostInterestToSavingsAccountValidator : AbstractValidator<PostInterestToSavingsAccountCommand>
{
    public PostInterestToSavingsAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
    }
}

public sealed class PostInterestToSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IDateTimeProvider dateTime)
    : ICommandHandler<PostInterestToSavingsAccountCommand>
{
    public async ValueTask<Unit> Handle(PostInterestToSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);

        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);
        account.PostInterest(cmd.AsOf, today);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
```

- [ ] **Step 2: Implement transactions query** — `GetSavingsAccountTransactions.cs`:

```csharp
using CoreBanking.Accounts.Application.Abstractions;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record SavingsTransactionDto(
    Guid Id,
    int TypeId,
    string Type,
    DateOnly TransactionDate,
    decimal Amount,
    decimal RunningBalance);

public sealed record GetSavingsAccountTransactionsQuery(Guid AccountId)
    : IQuery<IReadOnlyList<SavingsTransactionDto>>;

public sealed class GetSavingsAccountTransactionsHandler(ISavingsAccountReadRepository readRepo)
    : IQueryHandler<GetSavingsAccountTransactionsQuery, IReadOnlyList<SavingsTransactionDto>>
{
    public async ValueTask<IReadOnlyList<SavingsTransactionDto>> Handle(
        GetSavingsAccountTransactionsQuery query, CancellationToken ct)
    {
        return await readRepo.FindTransactionsAsync(query.AccountId, ct);
    }
}
```

- [ ] **Step 3: Extend read repository abstraction** — in `ISavingsAccountReadRepository.cs` add to the interface:

```csharp
    Task<IReadOnlyList<SavingsTransactionDto>> FindTransactionsAsync(Guid accountId, CancellationToken ct = default);
```

- [ ] **Step 4: Extend the DTO** — replace `SavingsAccountDto.cs` record with:

```csharp
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
    DateOnly? InterestPostedTillDate);
```

- [ ] **Step 5: Build to verify compile errors are only in Infrastructure** (read repo projection — fixed next task)

Run: `dotnet build services/savings-accounts/CoreBanking.Accounts.Application`
Expected: PASS (Application layer compiles; Infrastructure intentionally not built yet)

- [ ] **Step 6: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): post-interest command, transactions query, balance in DTO"
```

---

### Task 10: Infrastructure — EF mapping, repositories, migration

**Files:**
- Create: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/Configurations/SavingsAccountTransactionConfiguration.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/Configurations/SavingsAccountConfiguration.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/SavingsAccountsWriteDbContext.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/SavingsAccountsReadDbContext.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/SavingsAccountRepository.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/SavingsAccountReadRepository.cs`

- [ ] **Step 1: Transaction entity configuration** — `SavingsAccountTransactionConfiguration.cs`:

```csharp
using CoreBanking.Accounts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreBanking.Accounts.Infrastructure.Persistence.Configurations;

public sealed class SavingsAccountTransactionConfiguration : IEntityTypeConfiguration<SavingsAccountTransaction>
{
    public void Configure(EntityTypeBuilder<SavingsAccountTransaction> b)
    {
        b.ToTable("SAVINGS_ACCOUNT_TRANSACTIONS");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.AccountId).HasColumnName("ACCOUNTID");
        b.Property(x => x.Type).HasColumnName("TYPEENUM").HasConversion<int>();
        b.Property(x => x.TransactionDate).HasColumnName("TRANSACTIONDATE");
        b.Property(x => x.Amount).HasColumnName("AMOUNT").HasColumnType("NUMBER(19,6)");
        b.Property(x => x.RunningBalance).HasColumnName("RUNNINGBALANCE").HasColumnType("NUMBER(19,6)");
        b.HasIndex(x => new { x.AccountId, x.TransactionDate });
    }
}
```

- [ ] **Step 2: Extend account configuration** — in `SavingsAccountConfiguration.cs`, add before the `Version` line:

```csharp
        b.Property(x => x.AccountBalance).HasColumnName("ACCOUNTBALANCE").HasColumnType("NUMBER(19,6)");
        b.Property(x => x.Compounding).HasColumnName("COMPOUNDINGENUM").HasConversion<int>();
        b.Property(x => x.PostingPeriod).HasColumnName("POSTINGPERIODENUM").HasConversion<int>();
        b.Property(x => x.DaysInYear).HasColumnName("DAYSINYEARENUM").HasConversion<int>();
        b.Property(x => x.InterestPostedTillDate).HasColumnName("INTERESTPOSTEDTILLDATE");

        b.HasMany(x => x.Transactions)
            .WithOne()
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
```

(EF finds the `_transactions` backing field by convention.)

- [ ] **Step 3: Register in both DbContexts** — in `SavingsAccountsWriteDbContext.cs` add after `SavingsAccounts`:

```csharp
    public DbSet<SavingsAccountTransaction> SavingsTransactions => Set<SavingsAccountTransaction>();
```

and inside `OnModelCreating` after the `SavingsAccountConfiguration` line:

```csharp
        modelBuilder.ApplyConfiguration(new SavingsAccountTransactionConfiguration());
```

Make the same two additions in `SavingsAccountsReadDbContext.cs` (mirror its existing structure).

- [ ] **Step 4: Repository loads transactions** — replace `FindAsync` in `SavingsAccountRepository.cs`:

```csharp
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Accounts.Infrastructure.Persistence;

public sealed class SavingsAccountRepository(SavingsAccountsWriteDbContext db) : ISavingsAccountRepository
{
    public void Add(SavingsAccount account) => db.SavingsAccounts.Add(account);

    public async Task<SavingsAccount?> FindAsync(Guid id, CancellationToken ct = default)
        => await db.SavingsAccounts
            .Include(a => a.Transactions)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
}
```

- [ ] **Step 5: Read repository** — in `SavingsAccountReadRepository.cs`, extend the DTO projection (add the two new fields after `a.WithdrawnOn`):

```csharp
                a.WithdrawnOn,
                a.AccountBalance,
                a.InterestPostedTillDate))
```

and add the transactions query method:

```csharp
    public async Task<IReadOnlyList<SavingsTransactionDto>> FindTransactionsAsync(
        Guid accountId, CancellationToken ct = default)
    {
        return await db.Set<SavingsAccountTransaction>()
            .Where(t => t.AccountId == accountId)
            .OrderBy(t => t.TransactionDate).ThenBy(t => t.Id)
            .Select(t => new SavingsTransactionDto(
                t.Id, (int)t.Type, t.Type.ToString(),
                t.TransactionDate, t.Amount, t.RunningBalance))
            .ToListAsync(ct);
    }
```

(add `using CoreBanking.Accounts.Domain;` to the file's usings).

- [ ] **Step 6: Build the whole service**

Run: `dotnet build services/savings-accounts/CoreBanking.Accounts.Api`
Expected: PASS

- [ ] **Step 7: Generate the migration**

Run:
```bash
dotnet ef migrations add AddSavingsTransactions \
  --project services/savings-accounts/CoreBanking.Accounts.Infrastructure \
  --startup-project services/savings-accounts/CoreBanking.Accounts.Api \
  --context SavingsAccountsWriteDbContext
```
Expected: new migration under `Persistence/Migrations/` creating `SAVINGS_ACCOUNT_TRANSACTIONS` and adding the five account columns. Inspect the generated `Up()` — it must contain `CreateTable` for the transactions table and five `AddColumn` calls; no `DropTable`/`DropColumn`.

- [ ] **Step 8: Run all unit tests**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): persist savings transactions (EF mapping + migration)"
```

---

### Task 11: Integration events + outbox map

**Files:**
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Events/SavingsAccountIntegrationEvents.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Add integration events** — append to `SavingsAccountIntegrationEvents.cs`:

```csharp
public sealed record SavingsDepositedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    Guid TransactionId,
    DateOnly TransactionDate,
    decimal Amount,
    decimal BalanceAfter)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsWithdrawnIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    Guid TransactionId,
    DateOnly TransactionDate,
    decimal Amount,
    decimal BalanceAfter)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}

public sealed record SavingsInterestPostedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    Guid TransactionId,
    DateOnly PostedThrough,
    decimal Amount,
    decimal BalanceAfter)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}
```

- [ ] **Step 2: Extend the outbox map** — in `DependencyInjection.cs`, add cases to `DomainEventToIntegrationEventMap` before `_ => null`:

```csharp
            SavingsDeposited e => new SavingsDepositedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.TransactionId, e.On, e.Amount, e.BalanceAfter),
            SavingsWithdrawn e => new SavingsWithdrawnIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.TransactionId, e.On, e.Amount, e.BalanceAfter),
            SavingsInterestPosted e => new SavingsInterestPostedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.TransactionId, e.PostedThrough, e.Amount, e.BalanceAfter),
```

- [ ] **Step 3: Build**

Run: `dotnet build services/savings-accounts/CoreBanking.Accounts.Api`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): publish transaction integration events via outbox"
```

---

### Task 12: API endpoints

**Files:**
- Modify: `services/savings-accounts/CoreBanking.Accounts.Api/Controllers/SavingsAccountsController.cs`

- [ ] **Step 1: Add endpoints** — inside the controller class, after `Withdraw`:

```csharp
    /// <summary>Deposit money into an active savings account.</summary>
    /// <remarks>
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}/transactions?command=deposit</c>.
    /// Backdated deposits are allowed back to the interest posting pivot date.
    /// </remarks>
    /// <response code="200">Deposit recorded; returns the transaction id.</response>
    /// <response code="404">Account not found.</response>
    /// <response code="422">
    /// Business rule violation: <c>account.transaction.notactive</c>, <c>account.transaction.future</c>,
    /// <c>account.transaction.beforeactivation</c>, <c>account.transaction.beforepivot</c>,
    /// <c>account.transaction.amount.invalid</c>.
    /// </response>
    [HttpPost("{id:guid}/transactions/deposit")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deposit(Guid id, [FromBody] TransactionRequest body, CancellationToken ct)
    {
        var txId = await mediator.Send(
            new DepositToSavingsAccountCommand(id, body.TransactionDate, body.Amount), ct);
        return Ok(new { transactionId = txId });
    }

    /// <summary>Withdraw money from an active savings account.</summary>
    /// <remarks>
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}/transactions?command=withdrawal</c>.
    /// The balance may never go negative at any point in the transaction timeline
    /// (error code <c>account.balance.insufficient</c>).
    /// </remarks>
    /// <response code="200">Withdrawal recorded; returns the transaction id.</response>
    /// <response code="404">Account not found.</response>
    /// <response code="422">Insufficient balance or transaction-date rule violation.</response>
    [HttpPost("{id:guid}/transactions/withdraw")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> WithdrawMoney(Guid id, [FromBody] TransactionRequest body, CancellationToken ct)
    {
        var txId = await mediator.Send(
            new WithdrawFromSavingsAccountCommand(id, body.TransactionDate, body.Amount), ct);
        return Ok(new { transactionId = txId });
    }

    /// <summary>Calculate and post interest for all completed posting periods up to a date.</summary>
    /// <remarks>
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}?command=postInterest</c>.
    /// Idempotent: re-running with the same date posts nothing new.
    /// </remarks>
    /// <response code="204">Interest posted (or no completed period was pending).</response>
    /// <response code="404">Account not found.</response>
    /// <response code="422">Account not active (<c>account.postinterest.notactive</c>).</response>
    [HttpPost("{id:guid}/postinterest")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PostInterest(Guid id, [FromBody] PostInterestRequest body, CancellationToken ct)
    {
        await mediator.Send(new PostInterestToSavingsAccountCommand(id, body.AsOf), ct);
        return NoContent();
    }

    /// <summary>List the account's transactions in chronological order.</summary>
    /// <response code="200">Transactions returned (possibly empty list).</response>
    [HttpGet("{id:guid}/transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<SavingsTransactionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(Guid id, CancellationToken ct)
    {
        var txs = await mediator.Send(new GetSavingsAccountTransactionsQuery(id), ct);
        return Ok(txs);
    }
```

And after the existing request records at the bottom of the file:

```csharp
/// <summary>Request body for deposit and withdrawal operations.</summary>
/// <param name="TransactionDate">Value date of the transaction (not in the future).</param>
/// <param name="Amount">Strictly positive amount in the account currency.</param>
public sealed record TransactionRequest(DateOnly TransactionDate, decimal Amount);

/// <summary>Request body for the post-interest operation.</summary>
/// <param name="AsOf">Post interest for all posting periods ending on or before this date.</param>
public sealed record PostInterestRequest(DateOnly AsOf);
```

- [ ] **Step 2: Build**

Run: `dotnet build services/savings-accounts/CoreBanking.Accounts.Api`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): transaction and post-interest API endpoints"
```

---

### Task 13: Integration test — Oracle persistence round-trip

**Files:**
- Create: `services/savings-accounts/tests/CoreBanking.Accounts.IntegrationTests/SavingsTransactionsPersistenceTests.cs`

This is the first test in this project (the csproj already references `Testcontainers.Oracle`, `FluentAssertions`, EF Core, and both Infrastructure and Api projects).

- [ ] **Step 1: Write the test**

```csharp
using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.Oracle;
using Xunit;

namespace CoreBanking.Accounts.IntegrationTests;

public sealed class SavingsTransactionsPersistenceTests : IAsyncLifetime
{
    // Same image family as docker/docker-compose.yml's oracle-free service (gvenzl/oracle-free);
    // pinned to a slim tag so CI pulls a smaller layer than :latest.
    private readonly OracleContainer _oracle = new OracleBuilder()
        .WithImage("gvenzl/oracle-free:23-slim-faststart")
        .Build();

    public Task InitializeAsync() => _oracle.StartAsync();
    public Task DisposeAsync() => _oracle.DisposeAsync().AsTask();

    private DbContextOptions<SavingsAccountsWriteDbContext> Options =>
        new DbContextOptionsBuilder<SavingsAccountsWriteDbContext>()
            .UseOracle(_oracle.GetConnectionString())
            .Options;

    [Fact]
    public async Task Deposit_withdraw_postinterest_roundtrip_through_oracle()
    {
        var today = new DateOnly(2026, 6, 7);
        Guid accountId;

        await using (var ctx = new SavingsAccountsWriteDbContext(Options))
        {
            await ctx.Database.MigrateAsync();

            var account = SavingsAccount.SubmitApplication(
                Guid.NewGuid(), Guid.NewGuid(), "SA-IT-001", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
            account.Approve(new DateOnly(2026, 1, 1));
            account.Activate(new DateOnly(2026, 1, 1));
            account.Deposit(new DateOnly(2026, 1, 1), 1000m, today);
            account.WithdrawMoney(new DateOnly(2026, 2, 10), 200m, today);
            account.PostInterest(new DateOnly(2026, 3, 31), today);
            account.ClearDomainEvents(); // outbox interceptor is not wired in this raw context
            accountId = account.Id;

            ctx.SavingsAccounts.Add(account);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new SavingsAccountsWriteDbContext(Options))
        {
            var loaded = await ctx.SavingsAccounts
                .Include(a => a.Transactions)
                .SingleAsync(a => a.Id == accountId);

            // 1 deposit + 1 withdrawal + 3 monthly interest postings (Jan, Feb, Mar)
            loaded.Transactions.Should().HaveCount(5);
            loaded.InterestPostedTillDate.Should().Be(new DateOnly(2026, 3, 31));

            var last = loaded.Transactions
                .OrderBy(t => t.TransactionDate).ThenBy(t => t.Id).Last();
            loaded.AccountBalance.Should().Be(last.RunningBalance);
            loaded.AccountBalance.Should().BeGreaterThan(800m); // 1000 - 200 + interest
        }
    }
}
```

- [ ] **Step 2: Run the integration test** (requires Docker running)

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.IntegrationTests`
Expected: PASS (first run pulls the Oracle image; allow several minutes)

- [ ] **Step 3: Commit**

```bash
git add services/savings-accounts
git commit -m "test(accounts): oracle round-trip for savings transactions and interest"
```

---

### Task 14: Full verification + docs

- [ ] **Step 1: Run every test suite in the solution that this change can affect**

```bash
dotnet build CoreBanking.slnx
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.ArchTests
dotnet test tests/CoreBanking.ContractTests
```
Expected: all PASS. ArchTests guard the Clean Architecture dependency rule — new Domain files must not reference Application/Infrastructure (they don't: the interest engine lives in Domain and references nothing).

- [ ] **Step 2: Update the controller's class-level remarks table** — in `SavingsAccountsController.cs`, the class `<remarks>` describes the state machine; append one line to the narrative after the transitions list:

```
/// Once <c>Active</c>, the account accepts deposit/withdrawal transactions and
/// interest posting (see the transaction endpoints below). Money can never make
/// the balance negative; backdated entries are allowed back to the interest pivot date.
```

- [ ] **Step 3: Update IMPLEMENTATION_PLAN.md** — add a row/section noting the savings-accounts service now implements Fineract-derived transactions + interest posting (deposit, withdrawal, monthly/quarterly/biannual/annual posting, daily/monthly compounding, 360/365 day-count, forward-only pivot model).

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "docs: record savings transactions + interest posting capability"
```

---

# Self-Review (performed at plan time)

- **Spec coverage:** every functional rule in Part 1 maps to a task — transaction types/entity (T1), settings snapshot (T2), deposit rules incl. pivot + backdating (T3), withdrawal timeline check (T4), posting periods (T5), formulas + day-count + compounding (T6), sequential posting/idempotency/pivot (T7), commands/queries (T8-9), persistence (T10), events (T11), API (T12), end-to-end (T13).
- **Type consistency check:** `Deposit(DateOnly on, decimal amount, DateOnly today)` / `WithdrawMoney(...)` / `PostInterest(DateOnly asOf, DateOnly today)` used identically in domain (T3/T4/T7), handlers (T8/T9), and tests. `DailyBalanceSpan(From, Days, Balance)` consistent across T6 engine and tests. Enum names `InterestCompoundingPeriod/InterestPostingPeriod/DaysInYearType` consistent across T1/T2/T5/T6/T10.
- **Known small risks called out inline:** `IDateTimeProvider` namespace (T8 note) and EF backing-field convention (T10) each have a one-line verification instruction at the point of use. `DomainException.Code` was verified against the actual source at plan time.
