# Account-to-Account Money Transfers — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: invoke `corebanking-feature` and implement task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. This plan was produced by an understand→design→adversarial-review→completeness-critic analysis against the Apache Fineract `account` module and the live CoreBanking code; review/critic findings are already folded in as decisions below.

**Goal:** Let a client move money from one **active savings account** to another in a single, atomic, immediate operation — a savings→savings funds transfer. One command books a **withdrawal** on the source and a **deposit** on the destination, plus one **transfer link record** tying the two legs together, all in one database transaction.

**Architecture (settled):** Both accounts live in the **same `SAVINGS` schema and the same `SavingsAccountsWriteDbContext`**, so the transfer is a single `uow.SaveChangesAsync()` wrapping both legs + the link record + all outbox rows in **one Oracle transaction**. **No saga, no distributed transaction, no `SavingsAccount.Transfer` domain method** — no aggregate owns both accounts, so orchestration lives in the **Application handler**. The two legs **reuse the existing `WithdrawMoney`/`Deposit` domain methods** (which already enforce active-status, positive-amount, future/activation/pivot, and full-timeline insufficiency), and a new **`AccountTransfer` link aggregate** carries the "this is a transfer" semantics and raises a correlation event.

**Tech stack:** .NET 10, EF Core 10 + Oracle, martinothamar/Mediator (not MediatR), FluentValidation, Confluent.Kafka (outbox already wired), xUnit + FluentAssertions, Testcontainers.Oracle.

**Scope:** savings→savings only. **Out of v1:** loan legs, refund-by-transfer, transfer undo/reversal, standing instructions (recurring), cross-currency/FX, offices, withdrawal fees, min-required-balance / holds / lock-in / sub-status blocks (none of these are modeled in CoreBanking yet — see §7 parity gaps).

---

# Part 1 — Functional Specification (ported from Apache Fineract)

**Source:** `fineract-provider/.../portfolio/account/service/AccountTransfersWritePlatformServiceImpl.java` `create()` (lines 99–215), `data/AccountTransfersDataValidator.java`, `data/request/AccountTransferRequest.java`, `api/AccountTransfersApiResource.java`, `domain/{AccountTransferDetails,AccountTransferTransaction,AccountTransferType}.java`, `SavingsAccountTransactionType.java`, and `db/changelog/.../0001_initial_schema.xml` changesets 20 (`m_account_transfer_details`) and 23 (`m_account_transfer_transaction`).

## 1.1 What a Fineract savings→savings transfer actually does

`create()` is **one atomic command** that produces **two independent `SavingsAccountTransaction`s plus one link record**:

1. Validate the request.
2. Assemble the from-account → `handleWithdrawal` → `SavingsAccount.withdraw()` books a **WITHDRAWAL (id=2)** debit on the source.
3. Assemble the to-account → `handleDeposit` → `SavingsAccount.deposit()` books a **DEPOSIT (id=1)** credit on the destination.
4. Currency-equality check (Fineract does this *after* both legs, relying on the DB transaction to roll back).
5. Build the `AccountTransferDetails` + `AccountTransferTransaction` link pointing at both savings transactions, `saveAndFlush`.

### 1.1.1 Transaction-type fidelity — the critical clarification

`SavingsAccountTransactionType` ids: `DEPOSIT=1`, `WITHDRAWAL=2`, `INTEREST_POSTING=3`, `WITHDRAWAL_FEE=4`, then `INITIATE_TRANSFER=12`, `APPROVE_TRANSFER=13`, `WITHDRAW_TRANSFER=14`, `REJECT_TRANSFER=15`.

**The money movement books `DEPOSIT(1)` + `WITHDRAWAL(2)` — it does NOT use ids 12–15.** Those four belong to the **separate client/office portfolio-transfer lifecycle** (the `proposeTransfer`/`acceptTransfer` flow driving account statuses `TRANSFER_IN_PROGRESS=303` / `TRANSFER_ON_HOLD=304`), which is unrelated to account-to-account money movement. **Therefore the funds transfer is immediate, with no approval step.** The "this is a transfer" semantics live in the **link record** (`AccountTransferType.ACCOUNT_TRANSFER=1`), not in the transaction type.

→ **Decision:** CoreBanking reuses `Deposit(1)`/`Withdrawal(2)` and adds **no** new `SavingsTransactionType` ids. Reuse is the *more* Fineract-faithful choice (matches the real funds path) **and** the only safe one: `SavingsAccountTransaction.IsCredit` throws for any type outside `{Deposit, Withdrawal, InterestPosting}`, and the interest engine walks all transactions — new ids would force both to learn transfer semantics for zero behavioral gain.

## 1.2 Validations & exact Fineract error codes

| # | Rule | Fineract error code | CoreBanking v1 |
|---|------|---------------------|----------------|
| 1 | `transferAmount` not null and strictly > 0 | `validation.msg.accounttransfer.transferAmount.not.greater.than.zero` | Validator `Amount > 0` → 400 |
| 2 | `transferDescription` not blank, length ≤ 200 (but column is 100!) | `...transferDescription.exceeds.max.length` | Validator `MaximumLength(100)` → 400 (**reject, don't truncate**) |
| 3 | Source must be **ACTIVE (300)** | `error.msg.savingsaccount.transaction.account.is.not.active` | `WithdrawMoney` guard; pre-gated with source attribution |
| 4 | Destination must be **ACTIVE (300)** | `error.msg.savingsaccount.transaction.account.is.not.active` | **Pre-gated with destination attribution** (see §2.4) |
| 5 | `transferDate` not in the future (both legs) | `error.msg.savingsaccount.transaction.in.the.future` | `EnsureTransactionAllowed` (both legs) |
| 6 | `transferDate` not before each account's activation (independent per account) | `error.msg.savingsaccount.transaction.before.activation.date` | `EnsureTransactionAllowed`; pre-gated per leg |
| 7 | `transferDate` not on/before the interest pivot (per account) | `error.msg.savingsaccount.transaction.before.pivot.date` | **Pre-gated per leg** (see §2.3) |
| 8 | Same currency on both accounts | `error.msg.accounttransfer.different.currencies` | **Pre-gated** (moved *before* either leg) |
| 9 | Lock-in period blocks source debit | `...withdrawals.blocked.during.lockin.period` | Out of scope (not modeled) — §7 |
| 10 | Sub-status `BLOCK`/`BLOCK_DEBIT`/`BLOCK_CREDIT` | `error.msg.saving.account.blocked.transaction.not.allowed` | Out of scope — §7 |
| 11 | Insufficient funds (incl. min-balance + holds + fee) | `error.msg.savingsaccount.transaction.insufficient.account.balance` | **Non-negative-balance only** via `WithdrawMoney` timeline walk; min-balance/holds/fee not modeled — §7 |
| 12 | **No self-transfer guard exists in Fineract** | — | **Added in v1:** `account.transfer.from.to.same.account` → 400 |
| 13 | **No idempotency key exists in Fineract** | — | **Added in v1:** `ClientTransferReference` + unique index |

## 1.3 State & lifecycle rules

- Both accounts **ACTIVE (300)** at transfer time; any other status rejected.
- **Same-currency only** — no FX. Fineract checks *after* both legs; CoreBanking **moves this to a pre-gate** before booking either leg.
- Transfer is **single-step and immediate** — no pending/approval state for the money movement. The only link lifecycle is `is_reversed` (set by undo — deferred).
- `backdatedTxnsAllowedTill` is **hardcoded `false`** in Fineract's transfer path, so its pivot relaxation does not apply — transfers behave as forward operations. This matches CoreBanking's forward-only immutable pivot exactly.

## 1.4 Link entities (Fineract) → CoreBanking collapse

Fineract uses two tables: `m_account_transfer_details` (the link aggregate: from/to office+client+savings/loan FKs, `transfer_type`, optional standing instruction, owns a `List<AccountTransferTransaction>`) and `m_account_transfer_transaction` (one row per movement: `from_savings_transaction`, `to_savings_transaction`, `is_reversed`, amount, date, description ≤100).

→ **CoreBanking collapses both into one `ACCOUNT_TRANSFERS` table** because (a) office/client/loan dimensions are out of v1 scope (savings→savings only, and each account already owns its `ClientId`), and (b) one transfer = exactly two leg transactions referenced by id — no need for a `List`.

## 1.5 Worked examples (port these as tests)

- Balance 100.00, transfer 95.00, no min-balance → destination +95.00, source 5.00. **Allowed** (v1 has no min-balance).
- Balance 100.00, transfer 100.00 → source 0.00, destination +100.00. Allowed.
- Balance 100.00, transfer 100.01 → `account.balance.insufficient` (timeline walk goes negative).
- Same `sourceAccountId == destinationAccountId` → `account.transfer.from.to.same.account` (400), **before** any booking.
- `transferDate` on/before either account's `InterestPostedTillDate` → leg-named `before-pivot` 422, **before** any booking.

---

# Part 2 — Design Decisions

## 2.1 Atomicity — one UoW, no saga *(settled)*

`SavingsAccountUnitOfWork.SaveChangesAsync` is a single `db.SaveChangesAsync` on one scoped `SavingsAccountsWriteDbContext`. The handler loads source + destination through the **same scoped repo** (both EF-tracked), mutates both, adds the `AccountTransfer`, then **one** `SaveChangesAsync`. The `ConvertDomainEventsToOutboxInterceptor` writes `SavingsWithdrawn` + `SavingsDeposited` + `MoneyTransferred` to `OUTBOX_MESSAGES` **in the same transaction** as the two `SAVINGS_ACCOUNTS` UPDATEs, the two `SAVINGS_ACCOUNT_TRANSACTIONS` INSERTs, and the `ACCOUNT_TRANSFERS` INSERT. Any exception rolls back **everything** — no leg can commit alone. A saga would be distributed-transaction machinery for a purely local operation; **explicitly rejected**.

## 2.2 Orchestration in the handler, withdraw-first *(settled)*

No `SavingsAccount.Transfer` method (no aggregate owns two roots). The handler: load both → **pre-gate** → `source.WithdrawMoney(...)` **first** (insufficient-funds throws before the destination mutates) → `destination.Deposit(...)` → create `AccountTransfer` → one save. Reuse of the existing domain methods is a **stated principle** (don't re-derive the guards).

## 2.3 Pivot / backdating policy *(review-confirmed: correct)*

The transfer carries **one `transferDate` applied to both legs**. The two accounts have **independent `InterestPostedTillDate` pivots**. `EnsureTransactionAllowed` is **strict** (`on <= pivot` rejected), so the transfer is allowed only if `transferDate` is **strictly after both pivots**.

**The handler pre-checks both pivots before mutating either leg** and throws **leg-named** errors so the client knows which account blocked it:
- `transferDate <= source.InterestPostedTillDate` → `account.transfer.source.beforepivot` (message includes the source pivot date) → 422
- `transferDate <= destination.InterestPostedTillDate` → `account.transfer.destination.beforepivot` (message includes the destination pivot date) → 422

Without the pre-check, the withdraw-first leg would always surface the generic `account.transaction.beforepivot` and **mask** a destination-only violation. Forward-only immutability is preserved on both accounts.

## 2.4 Leg attribution for status & activation *(review fix — folded in)*

Active-status (#3/#4) and before-activation (#6) are **also per-account** checks. With withdraw-first ordering, a non-active or pre-activation **destination** would otherwise throw the generic `account.transaction.notactive` / `...beforeactivation` from the deposit leg with **no leg attribution** — the same masking problem §2.3 solves for pivots. → **The pre-gate also pre-checks destination active-status and before-activation**, throwing `account.transfer.destination.notactive` / `account.transfer.destination.beforeactivation`. The API 422 list (§4) enumerates **both** source and destination variants.

## 2.5 The `AccountTransfer` link aggregate *(settled)*

A **new `AggregateRoot`** (not a child of `SavingsAccount`) holding **by-id references only** (no EF navigation properties) to keep the two account aggregates independent: `SourceAccountId`, `DestinationAccountId`, `SourceTransactionId` (the withdrawal), `DestinationTransactionId` (the deposit), `Amount`, `CurrencyCode`, `TransferDate`, `Description`, `ClientTransferReference?`, plus `IAuditable`. Static factory `Create(...)` **rejects** `Description > 100` (throws — defense in depth behind the validator; **no silent truncation**) and **raises `MoneyTransferred`**. It must be an `AggregateRoot` so the outbox interceptor (which iterates `ChangeTracker.Entries<AggregateRoot>()`) picks up its event. Created in the handler **after both legs succeed** (it consumes the two returned tx ids).

## 2.6 Concurrency — fix the latent double-spend *(review: load-bearing)*

**Pre-existing bug (verified against source):** `AggregateRoot.Version` is `public int Version { get; private set; }` ([`AggregateRoot.cs:9`](../../../shared/CoreBanking.BuildingBlocks.Domain/AggregateRoot.cs)) with **no** increment method and no app code that mutates it, yet it **is** mapped `.IsConcurrencyToken()` ([`SavingsAccountConfiguration.cs:41`](../../../services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/Configurations/SavingsAccountConfiguration.cs)). EF Core does not auto-increment a plain `int` token on Oracle (no `rowversion` equivalent), so every UPDATE emits `... WHERE VERSION=0`, both concurrent updates match, and **no `DbUpdateConcurrencyException` ever fires**. Two concurrent transfers from the same source can each pass the in-memory insufficiency check and both commit → **lost update / double-spend**. This is not theoretical for a money-movement feature. (The current-state analysis claiming "locking works on the first update" was wrong — confirmed by reading both files.)

→ **Decision:** make `Version` actually increment. Use the **centralized** form: a `SaveChanges` interceptor (or extend the existing audit/outbox interceptor) that bumps `Version` for **every `EntityState.Modified` `AggregateRoot`**. This covers balance mutations **and** status-only transitions (Approve/Activate/Reject/**Close**), closing both the transfer double-spend window *and* the "deposit into a concurrently-closing destination" race (§ critic). When the source or destination changes between load and save, the loser's UPDATE matches 0 rows → `DbUpdateConcurrencyException` → existing `ExceptionToProblemDetailsHandler` → **409**.

> Blast radius: this changes optimistic-locking from "never fires" to "fires" for *all* existing deposit/withdraw/close flows. That is the correct behavior, but confirm before shipping (see §8). It is **code-only** — the `VERSION` column already exists on `SAVINGS_ACCOUNTS`.

## 2.7 Idempotency — client reference + unique index *(settled, payload-match added)*

Optional caller-supplied `ClientTransferReference` on the command, persisted with a **unique index** (Oracle stores no null keys, so a plain unique index is already "unique where not null" — no filter needed). The inbox does **not** apply (it dedupes inbound *Kafka events* by `eventId`; this is an inbound HTTP command).

Handler behavior *(critic fix — folded in)*:
- Reference present and a transfer with it **already exists**:
  - **payload matches** (same source, dest, amount) → return the existing transfer id (idempotent replay; **not** an error).
  - **payload differs** → `account.transfer.idempotency.conflict` → **422** (key reuse with different parameters).
- Reference present, no existing row, but a **concurrent duplicate** races the unique index → catch the unique-violation and **return the existing transfer id** (treat as idempotent replay, not a 409).
- Reference omitted → no dedupe (caller opts out).

## 2.8 Events — per-leg + correlation *(settled)*

The per-leg `SavingsWithdrawn` (source) and `SavingsDeposited` (destination) **still fire** — they are real, independent per-account ledger changes that downstream read models/statements legitimately consume (not double-eventing). **Additionally** publish a **`MoneyTransferred`** correlation event tying both legs + the transfer id together. Standard **3-part outbox change**:
1. Domain event `MoneyTransferred : IDomainEvent` (raised by `AccountTransfer.Create`).
2. `MoneyTransferredIntegrationEvent : IntegrationEvent`, `Topic = "savings-accounts.events"`, in `Infrastructure/Events/`.
3. A `case` in `DomainEventToIntegrationEventMap` (`Infrastructure/DependencyInjection.cs`).

`AggregateKey = TransferId` → lands on a different Kafka partition than the `AccountId`-keyed leg events (no co-partition ordering between a leg event and its correlation event). Acceptable for v1; flagged for consumer owners (§8).

## 2.9 Statement / transaction-history display *(critic fix — in v1 scope)*

The (correct) reuse of `Deposit`/`Withdrawal` means a transfer leg renders as a **bare deposit/withdrawal with no counterparty** in `GET /savingsaccounts/{id}/transactions`. A user/auditor can't tell a transfer from a manual deposit. → **v1 includes a read-side enrichment:** when projecting a savings account's transactions, **left-join `ACCOUNT_TRANSFERS`** on `SourceTransactionId`/`DestinationTransactionId` to add a `transfer` block to the transaction DTO — `{ transferId, direction: "out"|"in", counterpartyAccountId, counterpartyAccountNo }`. This requires indexes on the **transaction-id** columns (§3), not the account-id columns.

---

# Part 3 — Persistence (`ACCOUNT_TRANSFERS`, `SAVINGS` schema)

One table, GUID PK (`RAW(16)`, `Guid.CreateVersion7()`, `ValueGeneratedNever`). **No FK constraints** — `AccountTransfer` is a separate aggregate referencing two independent `SavingsAccount` roots, so cross-aggregate references are **by id only** (keeps each aggregate independently loadable; avoids accidental cascade). Same-schema FKs *would* be allowed, but the boundary here is **aggregate**, not schema. (Contrast `SAVINGS_ACCOUNT_TRANSACTIONS`, which *does* FK to `SAVINGS_ACCOUNTS` — that's a child *within* one aggregate.)

| Column | Oracle type | Null | Notes |
|--------|-------------|------|-------|
| `Id` | `RAW(16)` | no | PK, domain-assigned |
| `SOURCEACCOUNTID` | `RAW(16)` | no | by-id ref to source account |
| `DESTINATIONACCOUNTID` | `RAW(16)` | no | by-id ref to destination account |
| `SOURCETRANSACTIONID` | `RAW(16)` | no | by-id ref to the withdrawal tx — **indexed** (statement join) |
| `DESTINATIONTRANSACTIONID` | `RAW(16)` | no | by-id ref to the deposit tx — **indexed** (statement join) |
| `AMOUNT` | `NUMBER(19,6)` | no | money convention |
| `CURRENCYCODE` | `NVARCHAR2(3)` | no | both accounts share it |
| `TRANSFERDATE` | `NVARCHAR2(10)` | no | `DateOnly` → ISO string (schema convention; **not** Oracle `DATE`) |
| `DESCRIPTION` | `NVARCHAR2(100)` | no | rejected if >100 (no truncation) |
| `CLIENTTRANSFERREFERENCE` | `NVARCHAR2(100)` | yes | idempotency key; **unique index** |
| `VERSION` | `NUMBER(10)` | no | `IsConcurrencyToken()` (from `AggregateRoot`) |
| `CREATEDONUTC` / `CREATEDBY` / `LASTMODIFIEDONUTC` / `LASTMODIFIEDBY` | `TIMESTAMP(7) WITH TIME ZONE` / `NVARCHAR2(100)` | mixed | `IAuditable` |

**Indexes (v1):** `PK_ACCOUNT_TRANSFERS`; `IX_..._SOURCETRANSACTIONID`; `IX_..._DESTINATIONTRANSACTIONID` (both for the statement-enrichment join); `IX_..._CLIENTTRANSFERREFERENCE` (unique). **Defer** the `SOURCEACCOUNTID`/`DESTINATIONACCOUNTID` indexes until the per-account "list transfers" endpoint ships — don't ship dead indexes.

**EF config:** `AccountTransferConfiguration : IEntityTypeConfiguration<AccountTransfer>` in `Persistence/Configurations/`, registered in **both** `SavingsAccountsWriteDbContext` and `SavingsAccountsReadDbContext` (`ApplyConfiguration` + a `DbSet<AccountTransfer>`). No `HasColumnType` on `TransferDate` (provider auto-maps `DateOnly`→`NVARCHAR2(10)`). `HasIndex(x => x.ClientTransferReference).IsUnique()` (no `HasFilter`).

**Migration** (after `AccountTransfer.cs` exists and the solution builds — `TreatWarningsAsErrors` aborts `ef` on a missing type):
```bash
dotnet ef migrations add AddAccountTransfers \
  --project services/savings-accounts/CoreBanking.Accounts.Infrastructure \
  --startup-project services/savings-accounts/CoreBanking.Accounts.Api \
  --context SavingsAccountsWriteDbContext
```
Inspect `Up()`: one `CreateTable` (`ACCOUNT_TRANSFERS`, `SAVINGS`) + the three indexes, **no FKs**, **no** `AddColumn`/`DropTable` on existing tables, `TRANSFERDATE` is `NVARCHAR2(10)`, the unique index has no `WHERE` clause. `Down()` = `DropTable` only.

---

# Part 4 — API Surface

### `POST /api/v1/accounttransfers`
```jsonc
{
  "sourceAccountId": "<guid>",
  "destinationAccountId": "<guid>",
  "transferDate": "2026-06-29",          // ISO-8601; locale/dateFormat dropped
  "amount": 250.00,                       // scale must be <= currency decimal places (§ critic)
  "description": "Rent",                  // <= 100 chars
  "clientTransferReference": "abc-123"    // optional idempotency key
}
```
Dropped vs Fineract: `locale`/`dateFormat` (ISO only); `fromOfficeId`/`toOfficeId` (no Office model); `fromClientId`/`toClientId` (each account owns its `ClientId`); `from/toAccountType` (savings→savings only).

**Responses:** `201 Created` `{ id }` + `Location: /api/v1/accounttransfers/{id}` · `400` validation / self-transfer (`account.transfer.from.to.same.account`) / sub-currency-precision amount · `403` not authorized (§ Part 5) · `404` source or destination not found · `422` currency mismatch / source|**destination** before-pivot / source|**destination** not-active / before-activation / future date / insufficient funds / idempotency conflict · `409` optimistic concurrency (either account changed mid-transfer).

### `GET /api/v1/accounttransfers/{id:guid}`
`200` `AccountTransferDto` · `404`. Backs the POST `Location` header.

New controller `AccountTransfersController` (`[Route("api/v1/accounttransfers")]`, ctor-injected `IMediator`). Errors map to ProblemDetails automatically — no try/catch in the controller.

---

# Part 5 — Authorization, Audit & Ownership *(critic — Tier 1 decisions)*

These are the most security-sensitive parts of a money endpoint and are **currently absent** in the backend. v1 must address them explicitly:

- **Authorization:** AuthN middleware is wired (`Accounts.Api/Program.cs`, JwtBearer + `AddAuthorization`), but no controller carries `[Authorize]`. → **Add `[Authorize]` + a transfer policy/role** (e.g. permission `savings.transfer`) on `POST /accounttransfers`, and document the `403` shape. The UI already models capabilities (`CAN.*`/`RoleGuard`); mirror one for transfers.
- **Audit actor:** `ICurrentUser` is hardcoded to `SystemCurrentUser → "system"`, so every `CreatedBy` would be `"system"` — no real initiator on a money movement. → **Wire `ICurrentUser` to the JWT subject** as part of this feature so the transfer's `CreatedBy` is the real user. (Small change, high compliance value.)
- **Ownership/tenancy:** nothing checks the caller is entitled to debit the source client's account, and `GET /{id}` returns both account ids + amount to any authenticated user. → **Add an entitlement check** (caller may act on the source account's client) or **explicitly defer with a stated risk** in §8. Recommended: at minimum gate `GET` so only an authorized staff role reads transfers.

---

# Part 6 — Task Breakdown (TDD order)

Branch: `git checkout -b feature/account-to-account-transfers`. One commit per task.

- [ ] **Task 1 — Concurrency: real `Version` increment.** Test (Accounts.UnitTests): after a deposit/withdrawal **and** after a status transition (e.g. Close), `SavingsAccount.Version` increases. RED (stays 0) → add the centralized bump (SaveChanges interceptor over modified `AggregateRoot`s, or `Touch()` on `AggregateRoot` called by mutators) → GREEN. Run full Accounts.UnitTests for regressions. *Foundational: makes the later 409 real and closes the close-during-transfer race.*
- [ ] **Task 2 — `AccountTransfer` aggregate + `MoneyTransferred` event.** Test (`AccountTransferTests`): `Create(...)` sets all by-id fields, **rejects** `Description > 100`, raises `MoneyTransferred`. RED → add `Domain/AccountTransfer.cs` + `Domain/Events/AccountTransferEvents.cs` → GREEN.
- [ ] **Task 3 — Transfer handler orchestration** (in-memory, fake repos/uow, fixed `IDateTimeProvider`). Tests (`TransferBetweenSavingsAccountsHandlerTests`): (a) happy path books withdrawal-then-deposit + one `AccountTransfer`; (b) insufficient funds throws **before** destination mutates; (c) self-transfer → 400 (validator); (d) currency mismatch → 422; (e) source-before-pivot **and** destination-before-pivot → leg-named 422; (f) **destination-not-active / destination-before-activation** → leg-named 422; (g) amount with too many decimals → 400; (h) duplicate reference + matching payload → returns existing id; (i) duplicate reference + different payload → 422. RED → implement `Application/Accounts/TransferBetweenSavingsAccounts.cs` (command + validator + handler) + `Abstractions/IAccountTransferRepository.cs` (`Add`, `FindByIdAsync`, `FindByClientReferenceAsync`) → GREEN.
- [ ] **Task 4 — Read query.** Test for `GetAccountTransferByIdQuery`: entity→`AccountTransferDto`, NotFound → 404. RED → `Application/Accounts/GetAccountTransferById.cs` + read-repo projection → GREEN.
- [ ] **Task 5 — Persistence + integration** (Testcontainers Oracle). Tests (`AccountTransferPersistenceTests`): (a) success commits both account UPDATEs + `ACCOUNT_TRANSFERS` row + **3** outbox rows atomically; (b) forced failure rolls **all** back; (c) two concurrent transfers from the same source → second gets `DbUpdateConcurrencyException`/409; (d) duplicate reference → idempotent replay (not 409). RED → `AccountTransferConfiguration`, both `DbSet`s, migration (§3) → GREEN.
- [ ] **Task 6 — API + outbox map + contract.** Wire `AccountTransfersController` (POST + GET), `[Authorize]` + transfer policy (§5); add the `MoneyTransferred` case to `DomainEventToIntegrationEventMap` + `MoneyTransferredIntegrationEvent`; add a `ContractTests` assertion for the new event schema **and** one asserting the per-leg events are unchanged. Smoke-test the 201/400/403/404/422/409 mapping.
- [ ] **Task 7 — Statement enrichment** (§2.9). Test: a transferred-out leg renders with `transfer.direction="out"` + counterparty; a manual deposit renders with no transfer block. RED → extend the transactions projection with the left-join on `ACCOUNT_TRANSFERS` → GREEN.
- [ ] **Task 8 — Wire `ICurrentUser` to JWT subject** (§5) so transfer audit attribution is real. Test: `CreatedBy` reflects the authenticated subject, not `"system"`.
- [ ] **Task 9 — ArchTests + docs.** Confirm `*.ArchTests` stay green (new `AccountTransfer`/repository obey Domain→Application→Infrastructure). Update **`ARCHITECTURE.md`** (§9 below).
- [ ] **Task 10 — UI** (optional, follow-up): a "Transfer" action on an active account (source picker → destination picker → amount/date/description), calling `POST /accounttransfers`; show transfers in transaction history using the §2.9 enrichment.

---

# Part 7 — Fineract Parity Gaps (deferred, documented so QA doesn't flag them)

CoreBanking does not yet model these, so the transfer path legitimately omits them — **but insufficiency/eligibility parity therefore differs from Fineract**:

| Fineract behavior | Why deferred |
|---|---|
| `WITHDRAWAL_FEE(4)` auto-booked on transfer, **included in the insufficiency check** | No charges model |
| Minimum-required-balance / enforce-min-balance | Not modeled (v1 = non-negative only) |
| On-hold / lien funds (`savingsHoldAmount`) excluded from transferable balance | No holds model |
| Lock-in period blocks source debit | Not modeled |
| Sub-status `BLOCK`/`BLOCK_DEBIT`/`BLOCK_CREDIT` | No sub-status model |
| Zero-amount link persisted as NULL (`getAmountDefaultedToNullIfZero`) | Normalize to 0 in CoreBanking |

**Also deferred (future slices):** transfer **undo/reversal** (Fineract `is_reversed`; will need an `IS_REVERSED` column migration *and* resolution of the conflict with forward-only immutable pivots — reversing a past-pivot leg is currently forbidden); **standing instructions** (recurring transfers); **loan legs** / refund-by-transfer; **cross-currency/FX**; **offices**; per-account **list** + **template** endpoints (and their account-id indexes).

---

# Part 8 — Open Questions (confirm before/while building)

1. **`Version`-bump blast radius:** the centralized fix changes optimistic locking for *all* existing flows from "never fires" to "fires." Correct, but confirm it's acceptable now vs. a narrower scope. (The current-state note claiming "locking works on first update" is **wrong** — it never fires today; the fix is load-bearing for transfer race-safety.)
2. **Authorization model:** which role/permission gates `POST /accounttransfers`, and does `GET` need ownership scoping? (§5)
3. **`MoneyTransferred` partition key:** `TransferId` (no co-partition ordering with leg events) vs `SourceAccountId`. Recommend `TransferId` for v1; confirm with consumer owners.
4. **Description > 100:** reject (recommended) — confirmed in this plan; Fineract API allows 200 but its column is 100.
5. **List/template endpoints:** deferred (v1 = POST + GET{id} + statement enrichment). Confirm.

---

# Part 9 — `ARCHITECTURE.md` updates (same change — mandated)

- §2.1 endpoints: add `POST /api/v1/accounttransfers` and `GET /api/v1/accounttransfers/{id}` (+ gateway route for the new `accounttransfers` path segment).
- §2 service catalogue: add `MoneyTransferred` to the Savings Accounts **Publishes** column (topic `savings-accounts.events`).
- §4 events: note the `MoneyTransferred` domain→integration map case; per-leg events unchanged.
- §6 schema: add `ACCOUNT_TRANSFERS` to the `SAVINGS` schema table/ER (by-id refs, no FKs).
- Follow the end-of-file change checklist.

---

# Part 10 — Verification (evidence before "done")

```bash
dotnet build CoreBanking.slnx                                              # 0 errors (warnings fail)
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.ArchTests
dotnet test tests/CoreBanking.ContractTests
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.IntegrationTests   # needs Docker
```
If Docker is down, the integration suite fails at container construction — report it as "not executed," don't fold it into "all green."
