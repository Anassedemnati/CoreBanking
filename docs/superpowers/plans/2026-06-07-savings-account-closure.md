# Savings Account Closure Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** An active savings account (`Active`/300) can be permanently closed (`Closed`/600). Closure
asserts the close date is valid and the balance is zero, optionally sweeps any remaining balance to zero
via a pivot-exempt withdrawal dated the close date, sets `ClosedOn`, flips status to the terminal
`Closed` state, and publishes a `SavingsAccountClosed` integration event.

**Architecture:** Extend the existing `SavingsAccount` aggregate with a terminal `Close()` lifecycle
method and a `ClosedOn` stamp. The behaviour follows the established slice pattern: domain method →
domain event → outbox integration event → Kafka `savings-accounts.events`; CQRS command via Mediator;
Oracle persistence via EF Core with a new migration (one nullable `CLOSEDON` column). Closure reuses the
deposit/withdrawal/interest-posting timeline machinery built in
`2026-06-07-savings-transactions-interest-posting.md`.

**Tech Stack:** .NET 10, EF Core 10 + Oracle, martinothamar/Mediator, FluentValidation,
Confluent.Kafka (outbox already wired), xUnit + FluentAssertions, Testcontainers.Oracle.

**Document structure:** Part 1 (below) is the behavioural contract written by migration-po and verified
against Fineract. Part 2 is the technical design (migration-architect). Part 3 is the task-by-task,
TDD-first implementation plan. Parts 2–3 build strictly against Part 1; where the spec and a looser
phrasing collided, the spec wins and the reconciliation is recorded as a key design decision (§2.6).

---

# Part 1 — Functional Specification (ported from Apache Fineract)

## 1. Source map

All Fineract paths are absolute under `/Users/mac/Documents/Projects/fineract`. Quoted rules are from the
code as read, not paraphrased from memory.

| Concern | File | Method / lines |
|---|---|---|
| Domain `close()` tail (validations + status flip + `closedOnDate`) | `fineract-savings/src/main/java/org/apache/fineract/portfolio/savings/domain/SavingsAccount.java` | `close(AppUser, JsonCommand)` **2790–2855** |
| Orchestration (validate → linked-account → datatable → post-interest check → settle-to-zero withdrawal → domain close → events) | `fineract-provider/src/main/java/org/apache/fineract/portfolio/savings/service/SavingsAccountWritePlatformServiceJpaRepositoryImpl.java` | `close(Long, JsonCommand)` **1015–1098** |
| Close request validator (`validateClosing`) | `fineract-provider/src/main/java/org/apache/fineract/portfolio/savings/data/SavingsAccountTransactionDataValidator.java` | `validateClosing(JsonCommand, SavingsAccount)` **163–203** |
| Command handler (entity=`SAVINGSACCOUNT`, action=`CLOSE`) | `fineract-provider/src/main/java/org/apache/fineract/portfolio/savings/handler/CloseSavingsAccountCommandHandler.java` | `processCommand` **41–46** |
| Status enum (ids preserved) | `fineract-core/src/main/java/org/apache/fineract/portfolio/savings/domain/SavingsAccountStatusType.java` | `ACTIVE(300)` **32**, `CLOSED(600)` **37**, `hasStateOf` **65–67** |
| `isAfterBusinessDate` (future-date check, **strict**) | `fineract-core/src/main/java/org/apache/fineract/infrastructure/core/service/DateUtils.java` | **258–260** |
| Transaction `isAfter(date)` (last-txn check, **strict**) | `fineract-savings/src/main/java/org/apache/fineract/portfolio/savings/domain/SavingsAccountTransaction.java` | **673–675** |
| Post-interest-on-close exception | `fineract-savings/src/main/java/org/apache/fineract/portfolio/savings/exception/PostInterestClosingDateException.java` | code `error.msg.postInterest.notDone` **25–27** |

### Key code quotes

Domain `close()` — status precondition (2797–2803):
```java
final SavingsAccountStatusType currentStatus = getStatus();
if (!SavingsAccountStatusType.ACTIVE.hasStateOf(currentStatus)) {
    baseDataValidator.reset().failWithCodeNoParameterAddedToErrorCode("not.in.active.state");
    ...throw new PlatformApiDataValidationException(dataValidationErrors);
}
```
Date rules (2809–2833):
```java
if (DateUtils.isBefore(closedDate, getActivationDate())) {   // strict: == activation OK
    ...failWithCode("must.be.after.activation.date");
}
if (DateUtils.isAfterBusinessDate(closedDate)) {              // strict: == today OK
    ...failWithCode("cannot.be.a.future.date");
}
... accountTransaction = savingsAccountTransactions.get(last);
if (accountTransaction.isAfter(closedDate)) {                // strict: closedDate == lastTxnDate OK
    ...failWithCode("must.be.after.last.transaction.date");
}
```
Zero-balance + status flip + closedOnDate (2834–2852):
```java
if (getAccountBalance().doubleValue() != 0) {
    ...failWithCodeNoParameterAddedToErrorCode("results.in.balance.not.zero");
}
...
this.status = SavingsAccountStatusType.CLOSED.getValue();
...
this.closedOnDate = closedDate;
this.closedBy = currentUser;
```
Orchestration — settle-to-zero withdrawal reuses the **normal withdrawal** (no new transaction type),
dated `closedDate`, for the **full remaining balance** (1056–1072):
```java
if (isWithdrawBalance && account.getSummary().getAccountBalance(...).isGreaterThanZero()) {
    final BigDecimal transactionAmount = account.getSummary().getAccountBalance();
    ...
    this.savingsAccountDomainService.handleWithdrawal(account, fmt, closedDate, transactionAmount,
            paymentDetail, transactionBooleanValues, false);  // isAccountTransfer=false, isApplyWithdrawFee=false
}
final Map<String, Object> accountChanges = account.close(user, command);
```
Post-interest-on-close-date check (1037–1052) — requires an **existing, non-reversed interest-posting
transaction dated exactly `closedDate`**, else throws:
```java
if (isPostInterest) {
    boolean postInterestOnClosingDate = false;
    for (SavingsAccountTransaction t : account.getTransactions()) {
        if (t.isInterestPosting() && t.isNotReversed()
                && DateUtils.isEqual(closedDate, t.getTransactionDate())) {
            postInterestOnClosingDate = true; break;
        }
    }
    if (!postInterestOnClosingDate) throw new PostInterestClosingDateException();
}
```

---

## 2. Business rules

### 2.1 Closure preconditions and steps (modern CoreBanking order)

Fineract's orchestration runs the settle-to-zero withdrawal **before** the close-date validations
(domain `close()` runs last), so a bad close date there surfaces as a *withdrawal* error, not a clean
close error. **CoreBanking improves on this**: validate all close-date rules **first**, then settle,
then assert zero balance, then flip. Evaluation order (first failure wins):

1. **Status must be Active (300).** Otherwise `account.close.notactive`
   (Fineract `not.in.active.state`, 2798). This also makes a re-close attempt fail cleanly (§4).
2. **Close date not before activation.** `closedOn >= ActivatedOn` (Fineract `isBefore` is **strict** →
   `closedOn == ActivatedOn` is **allowed**). Otherwise `account.close.beforeactivation`
   (Fineract `must.be.after.activation.date`, 2809).
3. **Close date not in the future.** `closedOn <= today` (Fineract `isAfterBusinessDate` is **strict** →
   `closedOn == today` is **allowed**). Otherwise `account.close.future`
   (Fineract `cannot.be.a.future.date`, 2816). `today` is a parameter to the domain method, supplied by
   the handler from `IDateTimeProvider` (same clock-free pattern as `Deposit`/`PostInterest`).
4. **Close date not before the last transaction date.** If any transactions exist, `closedOn >= max(TransactionDate)`
   (Fineract `isAfter` is **strict** → `closedOn == lastTransactionDate` is **allowed**; the rule is
   "not *before* the last transaction", despite the misleading Fineract code name
   `must.be.after.last.transaction.date`, 2826). Otherwise `account.close.afterlasttransaction`.
   **Note:** this is checked against the timeline **before** the settle-to-zero withdrawal is inserted,
   so the sweep (dated `closedOn`) never trips its own rule.
5. **(Optional) settle-to-zero step** — only if `withdrawBalance == true` *and* `AccountBalance > 0`:
   withdraw the **full** remaining balance, dated `closedOn`, via the existing withdrawal path
   (Fineract type 2, **no new transaction type**). See §2.2 for the pivot-guard exemption.
6. **Zero-balance requirement.** After any settle step, `AccountBalance == 0`. Otherwise
   `account.close.balance.nonzero` (Fineract `results.in.balance.not.zero`, 2834). If
   `withdrawBalance == false` and the balance is non-zero, closure is rejected here — the caller must
   sweep funds first or pass `withdrawBalance=true`.
7. **Flip + stamp.** Set `ClosedOn = closedOn`, `Status = Closed (600)`, raise `SavingsClosed`.

### 2.2 Settle-to-zero (`withdrawBalance`) — design decision (load-bearing)

The settle step withdraws the full balance dated `closedOn`. But the transactions slice's
`EnsureTransactionAllowed` rejects any transaction with `on <= InterestPostedTillDate`
(`account.transaction.beforepivot`,
`SavingsAccount.cs` 182–184). **Collision:** the supported "post final interest, then sweep and close"
flow posts interest on a period-end date `P` (pivot becomes `P`), then closes on `P` with the sweep
dated `P == pivot` → a plain `WithdrawMoney` call would be **rejected by the pivot guard**. Because
close requires `closedOn >= lastTransactionDate` and the final interest posting sits at `P`, this is the
*normal* close-with-interest path, not a corner case.

**Decision:** the closure settlement withdrawal is **exempt from the pivot guard**. Rationale: the
account goes terminal immediately after; nothing posts after the sweep, so the "don't corrupt an
already-posted period" rationale behind the pivot guard does not apply. **Therefore the settle-to-zero
step must NOT call the public `WithdrawMoney`**; it is a distinct aggregate path inside `Close()` that
performs the same timeline insertion + negative-balance check + running-balance recompute but skips
status/future/activation/pivot validation (those are already enforced on `closedOn` in steps 1–4).
The settle still records a normal **Withdrawal (type 2)** transaction and (per §3) does **not** raise a
separate `SavingsWithdrawn` event — the single `SavingsClosed` event carries the closing balance.

### 2.3 Post-interest-on-close (`postInterestValidationOnClosure`) — DEFERRED, with reason

Fineract's `isPostInterest` flag (orchestration 1037–1052) does **not** post interest; it *asserts* that
an interest-posting transaction already exists dated exactly `closedOn`, and throws
`error.msg.postInterest.notDone` otherwise. Fineract produces such a transaction via **as-on / partial
interest posting** (posting interest for an incomplete trailing period up to an arbitrary date).

CoreBanking's `PostInterest` (`SavingsAccount.cs` 138–166) only posts **complete calendar periods dated
the period end** — it has **no as-on/partial posting** (explicitly Non-Goal #5 in the transactions
plan). Consequently this Fineract check could only ever pass when `closedOn` happens to equal a
posting-period boundary that was already posted — a coincidence, not a feature.

**Decision: defer the `postInterestValidationOnClosure` behaviour from v1.** It depends on as-on partial
posting, which is itself deferred. If a tester wants interest posted up to the close date, they call the
existing `POST /postinterest` first (it will post any complete periods), then close. Re-introducing the
flag is a follow-up that ships *with* as-on posting. Error code `account.close.postinterest.required`
is **reserved** for that future slice and is **not emitted in v1**.

### 2.4 What sets ClosedOn and flips status

The domain `Close()` method (the modern equivalent of `SavingsAccount.java` 2841–2852) is the only place
that writes `ClosedOn` and sets `Status = Closed`. Fineract also nulls `rejectedOnDate`/`withdrawnOnDate`
(2847–2850) — irrelevant in CoreBanking because those fields are never set on an Active account (the
state machine only reaches Active via Submitted→Approved→Active). No equivalent reset needed.

---

## 3. Error semantics

CoreBanking maps domain errors to **HTTP 422**. Codes follow the existing dotted `account.*` convention
(the transactions slice already uses `account.transaction.notactive`, `account.balance.insufficient`,
etc. — those are CoreBanking's *modernized* codes, not literal Fineract strings). Fineract's raw leaf
code is recorded for traceability.

| CoreBanking code (HTTP 422) | Meaning / trigger | Fineract raw code (source) |
|---|---|---|
| `account.close.notactive` | Account status is not Active (incl. re-close of a Closed account) | `not.in.active.state` (`SavingsAccount.java` 2799) |
| `account.close.beforeactivation` | `closedOn < ActivatedOn` | `must.be.after.activation.date` (2811) |
| `account.close.future` | `closedOn > today` | `cannot.be.a.future.date` (2818) |
| `account.close.afterlasttransaction` | `closedOn < max(TransactionDate)` | `must.be.after.last.transaction.date` (2828) |
| `account.close.balance.nonzero` | Balance ≠ 0 after the (optional) settle step | `results.in.balance.not.zero` (2835) |
| `account.balance.insufficient` | *(reuse)* settle-to-zero would drive the timeline negative — should be impossible (full balance sweep), defensive only | `InsufficientAccountBalanceException` (withdrawal path) |
| `account.close.postinterest.required` | **RESERVED, not emitted in v1** (see §2.3) | `error.msg.postInterest.notDone` (`PostInterestClosingDateException` 26) |

Application-layer (FluentValidation, also 422 via the standard pipeline): `closedOn` is required
(Fineract `notNull`, validator 181); `withdrawBalance` must be a boolean if present (validator 183–186).

---

## 4. State / lifecycle

```
Submitted(100) ──Approve──▶ Approved(200) ──Activate──▶ Active(300) ──Close──▶ Closed(600)  [TERMINAL]
     │                                                        ▲
     ├──Reject──▶ Rejected(500)                               └── (only Active may be closed)
     └──Withdraw─▶ Withdrawn(400)
```

- Closure is **only** valid from `Active(300)`; the result is `Closed(600)`, preserving Fineract status
  ids (`SavingsAccountStatusType.ACTIVE`/`CLOSED`). The CoreBanking `SavingsAccountStatus` enum already
  defines `Closed = 600`.
- **`Closed` is terminal in v1.** There is **no reopen path** in the enum or state machine. A second
  `Close()` (or any transaction/interest-posting) on a Closed account fails with the relevant
  `notactive` code — for `Close()` specifically, `account.close.notactive`. This gives **idempotency by
  rejection**: re-close is a clean 422, not a silent no-op and not a double-close.

---

## 5. Edge cases & worked examples

Assume `ActivatedOn = 2026-01-01`, `today = 2026-06-07`, USD (2 dp), 5% nominal, monthly posting unless
noted.

1. **Close with zero balance, no sweep.** Deposit 1000 (Jan 10), withdraw 1000 (Feb 1) → balance 0.
   `Close(closedOn=2026-02-01, withdrawBalance=false)`. Passes all date rules (`closedOn == lastTxnDate`
   allowed), balance already 0 → `ClosedOn=2026-02-01`, `Status=Closed`, one `SavingsClosed` event.

2. **Close with non-zero balance, `withdrawBalance=true`.** Deposit 1000 (Jan 10), no other txns,
   balance 1000. `Close(closedOn=2026-03-15, withdrawBalance=true)`. Settle step inserts a Withdrawal of
   **1000** dated 2026-03-15 → balance 0 → close succeeds. Timeline now: Deposit 1000, Withdrawal 1000,
   both before/at `closedOn`. The closing `SavingsClosed.BalanceAfter == 0`.

3. **Close with non-zero balance, `withdrawBalance=false`.** Same as (2) but flag false → rejected with
   `account.close.balance.nonzero`. No state change, no transaction added.

4. **Interest accrued but unposted at close.** Deposit 1000 (Jan 1); current date 2026-03-15; no
   `PostInterest` run yet. `Close(closedOn=2026-03-15, withdrawBalance=true)` sweeps 1000 and closes. The
   **partial/unposted interest is forfeited** — v1 does not auto-post on close (§2.3). To capture it, the
   caller runs `POST /postinterest` first (posts complete Jan + Feb periods), then sweeps the higher
   balance, then closes. **This is the documented v1 workflow.**

5. **Post final interest, then sweep and close (pivot collision — the reason §2.2 exists).** Deposit 1000
   (Jan 1). `PostInterest(asOf=2026-01-31)` posts 4.25 on Jan 31 → balance 1004.25,
   `InterestPostedTillDate = 2026-01-31`. `Close(closedOn=2026-01-31, withdrawBalance=true)`:
   `closedOn == lastTxnDate (Jan 31)` ✔, `closedOn == pivot`. The settle withdrawal of 1004.25 is dated
   Jan 31 == pivot. Because the settle path is **pivot-exempt** (§2.2), it succeeds; a plain
   `WithdrawMoney` would have thrown `account.transaction.beforepivot`. Balance → 0 → close succeeds.

6. **Close date == last transaction date.** Allowed (boundary; `isAfter` strict). See (1), (5).

7. **Close date == activation date.** Allowed (`isBefore` strict). E.g. account activated and closed same
   day with zero balance: `Close(closedOn=2026-01-01)` passes (2).

8. **Close date == today.** Allowed (`isAfterBusinessDate` strict). `closedOn = 2026-06-07` ✔;
   `closedOn = 2026-06-08` → `account.close.future`.

9. **Backdated close before a later transaction.** Deposit 1000 (Mar 10), withdraw 1000 (Mar 10),
   balance 0. `Close(closedOn=2026-02-01)` → `account.close.afterlasttransaction` (Mar 10 > Feb 1).

10. **Re-close / idempotency.** After a successful close, a second `Close(...)` →
    `account.close.notactive` (status is Closed, not Active). Likewise any `Deposit`/`WithdrawMoney`/
    `PostInterest` on a Closed account → its respective `notactive` code.

11. **Empty-account close.** Account activated, never transacted, balance 0. `Close(closedOn>=ActivatedOn,
    withdrawBalance=false)` succeeds; the "last transaction" rule is skipped (no transactions, mirroring
    Fineract's `size() > 0` guard, 2824).

---

## 6. Acceptance criteria

Given/When/Then a tester can turn into assertions. "active account" = Submitted→Approved→Active with
`ActivatedOn=2026-01-01`, `today=2026-06-07`.

1. **AC-1 Happy path, zero balance, no sweep**
   - Given an active account with balance 0 and last transaction dated 2026-02-01
   - When `Close(closedOn=2026-02-01, withdrawBalance=false)`
   - Then `Status == Closed`, `ClosedOn == 2026-02-01`, exactly one `SavingsClosed` event with `BalanceAfter == 0`.

2. **AC-2 Sweep settles to zero**
   - Given an active account with a single deposit of 1000 (no other txns)
   - When `Close(closedOn=2026-03-15, withdrawBalance=true)`
   - Then a Withdrawal (type 2) of 1000 dated 2026-03-15 exists, `AccountBalance == 0`, `Status == Closed`,
     `SavingsClosed.BalanceAfter == 0`, and **no** separate `SavingsWithdrawn` event is raised.

3. **AC-3 Non-zero balance without sweep is rejected**
   - Given an active account with balance 1000
   - When `Close(closedOn=2026-03-15, withdrawBalance=false)`
   - Then throws `DomainException` with code `account.close.balance.nonzero`; `Status` stays `Active`; no transaction added.

4. **AC-4 Not active**
   - Given an account in `Submitted` (or already `Closed`)
   - When `Close(...)`
   - Then throws `account.close.notactive`.

5. **AC-5 Future close date**
   - Given an active account, When `Close(closedOn=2026-06-08, today=2026-06-07, ...)`
   - Then throws `account.close.future`. **And** `closedOn == today` (2026-06-07) is **accepted**.

6. **AC-6 Before activation**
   - Given an active account (`ActivatedOn=2026-01-01`), When `Close(closedOn=2025-12-31, ...)`
   - Then throws `account.close.beforeactivation`. **And** `closedOn == ActivatedOn` is **accepted**.

7. **AC-7 Before last transaction date**
   - Given an active account with last transaction dated 2026-03-10, When `Close(closedOn=2026-02-01, ...)`
   - Then throws `account.close.afterlasttransaction`. **And** `closedOn == lastTransactionDate` is **accepted**.

8. **AC-8 Pivot-exempt sweep (post interest then close same day)**
   - Given an active account, deposit 1000 (Jan 1), `PostInterest(asOf=2026-01-31)` posting 4.25 (pivot=Jan 31)
   - When `Close(closedOn=2026-01-31, withdrawBalance=true)`
   - Then the sweep of 1004.25 dated Jan 31 succeeds (no `account.transaction.beforepivot`), balance 0, `Status == Closed`.

9. **AC-9 Idempotency by rejection**
   - Given a successfully closed account, When `Close(...)` again
   - Then throws `account.close.notactive`; no second event, no field change.

10. **AC-10 Integration event published**
    - Given AC-1/AC-2, Then a `SavingsClosedIntegrationEvent` lands in the outbox in the same transaction
      and is published to `savings-accounts.events` (mirrors the 3 existing transaction events).

11. **AC-11 Empty account closes**
    - Given an active account with no transactions and balance 0, When `Close(closedOn>=2026-01-01)`
    - Then `Status == Closed` (last-transaction rule skipped).

---

## 7. Backlog (ordered vertical slices)

Confirms and refines the adviser's three slices. Each is independently shippable and TDD-first.

- **Slice 1 — Domain `Close()` + terminal transition + `ClosedOn`.**
  Add `ClosedOn` (`DateOnly?`) to the aggregate; `Close(DateOnly closedOn, DateOnly today)` enforcing
  rules §2.1 steps 1–4, 6–7 (status, beforeactivation, future, afterlasttransaction, zero-balance,
  flip to `Closed`, stamp `ClosedOn`); add `SavingsClosed` domain event. **No sweep yet** — caller must
  pre-zero the balance. Unit tests: AC-1, AC-3 (balance non-zero rejected), AC-4..AC-7, AC-9, AC-11.

- **Slice 2 — `withdrawBalance` settle-to-zero option.**
  Add the `withdrawBalance` parameter and the **pivot-exempt** internal settle path (§2.2): when
  `withdrawBalance && AccountBalance > 0`, insert a Withdrawal (type 2) for the full balance dated
  `closedOn` (timeline insert + negative-balance guard + running-balance recompute, skipping
  status/date/pivot validation already covered), then continue to the zero-balance assertion. Unit
  tests: AC-2, AC-8 (pivot exemption).

- **Slice 3 — Application command + API + integration event.**
  `CloseSavingsAccountCommand(Guid AccountId, DateOnly ClosedOn, bool WithdrawBalance = false)` +
  Mediator handler (supplies `today` from `IDateTimeProvider`); FluentValidation (`ClosedOn` required;
  `WithdrawBalance` boolean — trivially satisfied by the type); `SavingsClosedIntegrationEvent` mapped in
  the outbox DI map; `POST /savingsaccounts/{id}/close` MVC endpoint. Integration test: AC-10
  (event in outbox / Oracle round-trip of `ClosedOn` + status).

---

## 8. Scope decisions (v1 vs deferred)

**In v1:** Active→Closed transition; the five close-date/balance validations; the optional
`withdrawBalance` settle-to-zero step (pivot-exempt); `ClosedOn`; `SavingsClosed` integration event;
re-close rejected as `notactive`.

**Deferred — each is a decision with a Fineract anchor, not an oversight:**

1. **`postInterestValidationOnClosure` / post-interest-on-close-date.** Depends on as-on/partial interest
   posting (transactions plan Non-Goal #5), which CoreBanking does not have. Orchestration 1037–1052;
   exception `error.msg.postInterest.notDone`. Workflow substitute: run `POST /postinterest` then close
   (§2.3, edge case 4). Code `account.close.postinterest.required` reserved for the follow-up slice.
2. **Withhold-tax-on-close.** Fineract withholds tax as part of broader close flows (withhold tax is
   txn type 18, deferred in the transactions plan). No tax engine in CoreBanking v1.
3. **Transfer-on-close / `isAccountTransfer`.** The settle withdrawal hard-codes `isAccountTransfer=false`
   (orchestration 1062); account-to-account transfer-on-close is out of scope (no transfers in v1,
   transactions plan Non-Goal #3).
4. **Linked-active-account guard.** `SavingsAccountClosingNotAllowedException("linked", ...)`
   (orchestration 1021–1027). CoreBanking has no loan/account-association read model; deferred until
   such a local `*_REF` read model exists.
5. **Standing-instruction disable-on-close.** `disableStandingInstructionsLinkedToClosedSavings`
   (orchestration 1089). No standing-instructions feature in CoreBanking; deferred.
6. **Entity datatable checks.** `entityDatatableChecksWritePlatformService.runTheCheckForProduct(...)`
   (orchestration 1029–1030). CoreBanking has no datatable/custom-field framework; deferred.
7. **Hold-amount and blocked-substatus guards.** Validator 188–198
   (`amount.is.on.hold.release.the.amount.to.continue`, `account.is.in.blocked.state`). No holds or
   sub-status in CoreBanking v1 (transactions plan Non-Goal #3); deferred.
8. **Payment-detail capture.** Validator 200–222 + `createAndPersistPaymentDetail` (orchestration 1060).
   The close API in v1 takes only `closedOn` + `withdrawBalance`; payment-type/cheque/routing detail is
   deferred.
9. **Client/group transfer-date guard.** `validateActivityNotBeforeClientOrGroupTransferDate`
   (`SavingsAccount.java` 2840, 2857–2864). No client-office-transfer concept in CoreBanking; deferred.
10. **Note / audit `closedBy`.** Fineract stamps `closedBy` and an optional free-text note
    (`SavingsAccount.java` 2852; orchestration 1078–1083). CoreBanking captures actor via its standard
    audit/outbox metadata, not a domain `ClosedBy` field; the note parameter is deferred.
11. **Reopening a closed account.** No reopen path exists in Fineract's savings state machine for `Closed`
    (the enum, `SavingsAccountStatusType.java`); `Closed` is terminal. Confirmed deferred — re-close
    returns `account.close.notactive`.

---

# Part 2 — Technical Design

> **Role:** migration-architect. This part fixes the *shape* of the change — files, data model, the
> aggregate method signature, the event/outbox wiring, and the trade-offs — against the Part 1
> behavioural contract. The DBA translates §2.2 to Oracle DDL; the dev implements §2.3–2.7 test-first
> per Part 3.

The whole feature lives in the **`savings-accounts`** service. No new aggregate, no new service, no new
topic, no consumed events: closure is a new terminal **lifecycle transition** on the existing
`SavingsAccount` aggregate root, mirroring `Approve`/`Activate`/`Reject`/`Withdraw`. It reuses the
transaction timeline (`SavingsAccountTransaction`, running-balance recompute) shipped by the
transactions/interest slice — nothing about the aggregate boundary changes.

## 2.1 File structure

All paths relative to `/Users/mac/Documents/Projects/CoreBanking/services/savings-accounts/`.

**Modify — Domain:**
- `CoreBanking.Accounts.Domain/SavingsAccountStatus.cs` — `Closed = 600` (already present; verify, no change expected)
- `CoreBanking.Accounts.Domain/SavingsAccount.cs` — `ClosedOn` property; `Close(DateOnly closedOn, bool withdrawBalance, DateOnly today)`; an internal pivot-exempt settle helper extracted from the shared timeline mechanics
- `CoreBanking.Accounts.Domain/Events/SavingsAccountEvents.cs` — `SavingsAccountClosed` domain event

**Create — Application:**
- `CoreBanking.Accounts.Application/Accounts/CloseSavingsAccount.cs` — command + validator + handler

**Modify — Application:**
- `CoreBanking.Accounts.Application/Accounts/SavingsAccountDto.cs` — add `ClosedOn`

**Create — Infrastructure:**
- `CoreBanking.Accounts.Infrastructure/Persistence/Migrations/<timestamp>_AddSavingsAccountClosedOn.cs` (generated by migration-dba)

**Modify — Infrastructure:**
- `Persistence/Configurations/SavingsAccountConfiguration.cs` — map `ClosedOn` → `CLOSEDON`
- `Persistence/SavingsAccountReadRepository.cs` — project `ClosedOn` into the DTO
- `Events/SavingsAccountIntegrationEvents.cs` — `SavingsAccountClosedIntegrationEvent`
- `DependencyInjection.cs` — `SavingsAccountClosed` → `SavingsAccountClosedIntegrationEvent` map case

**Modify — Api:**
- `CoreBanking.Accounts.Api/Controllers/SavingsAccountsController.cs` — `POST {id}/close` action + `CloseAccountRequest` record

**Tests:**
- `tests/CoreBanking.Accounts.UnitTests/SavingsAccountCloseTests.cs` (create — domain rules + sweep)
- `tests/CoreBanking.Accounts.UnitTests/Handlers/CloseSavingsAccountHandlerTests.cs` (create — orchestration)
- `tests/CoreBanking.Accounts.IntegrationTests/SavingsAccountClosePersistenceTests.cs` (create — Oracle round-trip of `ClosedOn` + status)

## 2.2 Data model

One new **nullable** column on the existing table; no new table. (migration-dba owns the Oracle/EF
translation and runs the migration during the build — the model-level spec is below.)

```
SAVINGS.SAVINGS_ACCOUNTS            (existing, + one column)
  CLOSEDON   DATE   NULL            -- set once, when status flips to Closed (600); null while open
```

- `ClosedOn` is `DateOnly?` on the aggregate, mapped `HasColumnName("CLOSEDON")` exactly like the
  existing nullable lifecycle dates (`ApprovedOn`/`ActivatedOn`/`RejectedOn`/`WithdrawnOn`). No length,
  no precision — EF maps `DateOnly?` to Oracle `DATE`, same as its siblings.
- No transaction-table change. The optional settle records a normal **Withdrawal (type 2)** row in the
  existing `SAVINGS_ACCOUNT_TRANSACTIONS` table via the same insert path; closure adds no new
  transaction type and no new transaction column.
- The concurrency `Version` token already guards the aggregate; closure increments it like any mutation.

**Migration:** `AddSavingsAccountClosedOn` — a single `AddColumn` for `CLOSEDON` (nullable), no
`DropColumn`/`DropTable`. migration-dba generates it via `dotnet ef migrations add` against
`SavingsAccountsWriteDbContext` (Task 4).

## 2.3 Status enum + state machine

`SavingsAccountStatus` already defines `Closed = 600` (verified — `SavingsAccountStatus.cs`, line 10),
preserving Fineract's status id. The only new transition:

```
Active(300) ──Close──▶ Closed(600)   [TERMINAL]
```

- Closure is valid **only** from `Active`. From any other status `Close()` throws
  `account.close.notactive`.
- `Closed` is **terminal in v1** — no reopen. A re-close, or any `Deposit`/`WithdrawMoney`/`PostInterest`
  after close, fails with its respective `notactive` code (the existing `EnsureTransactionAllowed`
  already rejects non-Active for transactions; `Close` rejects non-Active itself). This is
  *idempotency by rejection* (Part 1 §4, AC-9).

## 2.4 Domain — `Close(DateOnly closedOn, bool withdrawBalance, DateOnly today)`

> **Signature note:** the prompt sketched `Close(closedOn, today)` with the sweep orchestrated by the
> handler calling `WithdrawMoney`. That is **not viable** against the spec — see §2.6 reconciliation.
> The `withdrawBalance` flag and the sweep live **inside** `Close()`.

Clock-free (`today` is a parameter, supplied by the handler from `IDateTimeProvider` — identical to
`Deposit`/`WithdrawMoney`/`PostInterest`). Evaluation order (first failure wins), matching Part 1 §2.1:

1. **Status Active** else `account.close.notactive`.
2. **Not before activation:** `closedOn >= ActivatedOn` (strict `<` fails) else `account.close.beforeactivation`.
3. **Not future:** `closedOn <= today` (strict `>` fails) else `account.close.future`.
4. **Not before last transaction:** if `_transactions` non-empty, `closedOn >= max(TransactionDate)`
   else `account.close.afterlasttransaction`. Checked against the timeline **before** the settle insert,
   so the sweep (dated `closedOn`) cannot trip its own rule.
5. **(Optional) settle:** if `withdrawBalance && AccountBalance > 0`, run the **pivot-exempt internal
   settle** for the full `AccountBalance`, dated `closedOn` (§2.6). It records a Withdrawal (type 2),
   recomputes running balances, and raises **no** `SavingsWithdrawn` event.
6. **Zero balance:** `AccountBalance == 0` else `account.close.balance.nonzero`.
7. **Flip + stamp:** `Status = Closed`, `ClosedOn = closedOn`, `Raise(new SavingsAccountClosed(Id,
   closedOn, AccountBalance))` (balance is 0 here — carried as `BalanceAfter` for parity with the other
   transaction events and AC-2/AC-10).

**Reuse, not reinvention.** The negative-balance timeline replay + `RebuildRunningBalances` already
exist inside `WithdrawMoney` (`SavingsAccount.cs` 114–136). Extract the shared mechanics into a private
helper so both the guarded public `WithdrawMoney` and the unguarded internal settle call it:

```csharp
// Inserts a withdrawal candidate, asserts the full-timeline balance never goes negative,
// commits it, and recomputes running balances. Raises NO event and runs NO status/date/pivot
// guards — callers decide which guards apply.
private SavingsAccountTransaction InsertWithdrawalUnchecked(DateOnly on, decimal amount)
{
    var candidate = SavingsAccountTransaction.Create(
        Id, SavingsTransactionType.Withdrawal, on, amount, NextTransactionSequence());
    decimal balance = 0m;
    foreach (var t in _transactions.Append(candidate)
                 .OrderBy(x => x.TransactionDate).ThenBy(x => x.Sequence))
    {
        balance += t.IsCredit ? t.Amount : -t.Amount;
        if (balance < 0m)
            throw new DomainException("account.balance.insufficient",
                $"Insufficient balance for a withdrawal of {amount} on {on:yyyy-MM-dd}.");
    }
    _transactions.Add(candidate);
    RebuildRunningBalances();
    return candidate;
}
```

`WithdrawMoney` becomes: `EnsureTransactionAllowed` + `EnsurePositive` + `InsertWithdrawalUnchecked` +
`Raise(SavingsWithdrawn)`. The settle inside `Close` calls `InsertWithdrawalUnchecked(closedOn,
AccountBalance)` directly — **pivot-exempt** because it bypasses `EnsureTransactionAllowed` (which is
where the `account.transaction.beforepivot` guard lives, `SavingsAccount.cs` 182–184). The negative
guard remains as a defensive check only (a full-balance sweep can't overdraw — Part 1 §3 row 6).

## 2.5 Application, API, Infrastructure surface

**CQRS command (`CloseSavingsAccount.cs`):**

```csharp
public sealed record CloseSavingsAccountCommand(
    Guid AccountId,
    DateOnly ClosedOn,
    bool WithdrawBalance = false) : ICommand;          // no payload; 204 on success
```

- **Validator:** `RuleFor(x => x.AccountId).NotEmpty();`. `ClosedOn` is a non-nullable `DateOnly` (the
  Fineract `notNull` rule is satisfied by the type); `WithdrawBalance` boolean is satisfied by the type
  (Part 1 §3 application-layer notes). The substantive close-date/balance rules are **domain**
  invariants (422 via the domain-exception pipeline), not validator rules.
- **Handler:** load with `repo.FindAsync` (which already `Include`s `Transactions`) → `NotFoundException`
  if null → `today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime)` →
  `account.Close(cmd.ClosedOn, cmd.WithdrawBalance, today)` (single call; **does not** touch
  `WithdrawMoney`) → `uow.SaveChangesAsync`. Same shape as `WithdrawFromSavingsAccountHandler`.

**API:** `POST /api/v1/savingsaccounts/{id}/close`, body `CloseAccountRequest(DateOnly ClosedOn, bool
WithdrawBalance = false)`. ProblemDetails mapping:
- `204 No Content` on success (no body, like `postinterest`).
- `404` account not found.
- `422` business-rule violation: `account.close.notactive`, `account.close.beforeactivation`,
  `account.close.future`, `account.close.afterlasttransaction`, `account.close.balance.nonzero`
  (and defensively `account.balance.insufficient`).

**Integration event + outbox (the 3-part wiring):**
1. Domain event `SavingsAccountClosed(Guid AccountId, DateOnly ClosedOn, decimal BalanceAfter)` in
   `SavingsAccountEvents.cs`.
2. `SavingsAccountClosedIntegrationEvent` in `SavingsAccountIntegrationEvents.cs`, with
   `Topic => "savings-accounts.events"` and `AggregateKey => AccountId.ToString()` (identical to every
   other savings event).
3. A `SavingsAccountClosed e => new SavingsAccountClosedIntegrationEvent(...)` case in
   `DomainEventToIntegrationEventMap` (`DependencyInjection.cs`), before `_ => null`.

## 2.6 Key design decisions & trade-offs

1. **Sweep is an internal pivot-exempt path inside `Close()`, not a handler-orchestrated `WithdrawMoney`
   (reconciliation, load-bearing).** A loose reading suggested the handler call the public
   `WithdrawMoney(closedOn, balance, today)` then `Close(closedOn, today)`. **The spec rejects this on
   three independent counts**, so the design follows the spec:
   - *Pivot collision (AC-8):* in the post-interest-then-close path `pivot == closedOn`, and public
     `WithdrawMoney` runs `EnsureTransactionAllowed`, whose pivot guard (`on <= pivot`) throws
     `account.transaction.beforepivot`. Part 1 §2.2 + AC-8 require the sweep to **succeed** here. The
     internal settle bypasses that guard (§2.4).
   - *No second event (AC-2):* public `WithdrawMoney` raises `SavingsWithdrawn`; AC-2 requires **no**
     separate withdrawal event — the single `SavingsAccountClosed` carries the closing balance. The
     internal settle raises nothing.
   - *Validation ordering (§2.1):* CoreBanking's stated improvement over Fineract is "validate all
     close-date rules first, then settle". Handler-orchestrated `WithdrawMoney`-then-`Close` settles
     before the close-date validations run, reintroducing the Fineract bug §2.1 fixes.

   Reuse is honoured at the *mechanics* level (`InsertWithdrawalUnchecked`), not the guarded public
   entry point — so there is no duplicated timeline/running-balance logic.

2. **Event naming reconciled to the lifecycle convention.** Part 1 prose says "`SavingsClosed`"; the
   actual lifecycle events are `SavingsAccountSubmitted`/`Approved`/`Activated`/`Rejected`/`Withdrawn`.
   Closure is a lifecycle transition, so the domain event is **`SavingsAccountClosed`** and the
   integration event **`SavingsAccountClosedIntegrationEvent`** — matching the lifecycle family, not the
   transaction-event family (`SavingsDeposited`/`SavingsWithdrawn`/`SavingsInterestPosted`). Behaviour
   (one event on `savings-accounts.events`, keyed by `AccountId`) is unchanged from the spec.

3. **`ClosedOn` is the only persisted addition.** No `ClosedBy` field — actor is captured by the
   standard audit/outbox metadata (`IAuditable`), matching Part 1 §8 item 10.

4. **Post-interest-on-close is DEFERRED** (Part 1 §2.3, §8 item 1). `Close()` does **not** post interest;
   the documented workflow is `POST /postinterest` then `POST /close`. Code
   `account.close.postinterest.required` is **reserved, not emitted in v1**.

5. **`today` is a parameter** — domain stays clock-free and unit-testable; handler supplies it from
   `IDateTimeProvider`. Consistent with every other dated domain method.

## 2.7 Event flow

```
SavingsAccount.Close()  → SavingsAccountClosed ─ ConvertDomainEventsToOutbox Interceptor (DI map)
                          ───────────────────────→ OutboxMessage (same txn) → Kafka savings-accounts.events
```

---

# Part 3 — Tasks

> Run all commands from `/Users/mac/Documents/Projects/CoreBanking`.
> Unit tests: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests`

### Task 0: Branch

- [ ] **Step 1: Create a feature branch** (skip if already on `feature/savings-account-closure`)

```bash
git checkout -b feature/savings-account-closure
```

---

### Task 1: Domain — `Close()` + terminal transition + `ClosedOn` (no sweep)

Slice 1 (Part 1 §7): the close-date/balance rules, the flip, the stamp, the domain event. The caller
must pre-zero the balance — `withdrawBalance` is wired but the sweep arrives in Task 2.

**Files:**
- Modify: `services/savings-accounts/CoreBanking.Accounts.Domain/SavingsAccount.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Domain/Events/SavingsAccountEvents.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/SavingsAccountCloseTests.cs` (create)

- [ ] **Step 1: Write the failing tests**

```csharp
using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Domain.Events;
using CoreBanking.BuildingBlocks.Domain;
using FluentAssertions;
using Xunit;

namespace CoreBanking.Accounts.UnitTests;

public sealed class SavingsAccountCloseTests
{
    private static readonly DateOnly Today = new(2026, 6, 7);

    private static SavingsAccount MakeActive(DateOnly? activatedOn = null)
    {
        var a = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-CLOSE", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        a.Approve(new DateOnly(2026, 1, 1));
        a.Activate(activatedOn ?? new DateOnly(2026, 1, 1));
        a.ClearDomainEvents();
        return a;
    }

    [Fact] // AC-1
    public void Close_zero_balance_no_sweep_flips_status_stamps_date_and_raises_event()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);
        a.WithdrawMoney(new DateOnly(2026, 2, 1), 1000m, Today);   // balance 0
        a.ClearDomainEvents();

        a.Close(new DateOnly(2026, 2, 1), withdrawBalance: false, Today);

        a.Status.Should().Be(SavingsAccountStatus.Closed);
        a.ClosedOn.Should().Be(new DateOnly(2026, 2, 1));
        a.DomainEvents.OfType<SavingsAccountClosed>().Should().ContainSingle()
            .Which.BalanceAfter.Should().Be(0m);
    }

    [Fact] // AC-11
    public void Close_empty_account_skips_last_transaction_rule()
    {
        var a = MakeActive();
        a.Close(new DateOnly(2026, 1, 1), withdrawBalance: false, Today);
        a.Status.Should().Be(SavingsAccountStatus.Closed);
    }

    [Fact] // AC-3
    public void Close_nonzero_balance_without_sweep_is_rejected()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);
        a.ClearDomainEvents();

        var act = () => a.Close(new DateOnly(2026, 3, 15), withdrawBalance: false, Today);

        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.close.balance.nonzero");
        a.Status.Should().Be(SavingsAccountStatus.Active);
        a.Transactions.Should().HaveCount(1);            // no transaction added
        a.DomainEvents.Should().BeEmpty();
    }

    [Fact] // AC-4 (submitted) + AC-9 (re-close)
    public void Close_on_non_active_throws_notactive()
    {
        var submitted = SavingsAccount.SubmitApplication(
            Guid.NewGuid(), Guid.NewGuid(), "SA-X", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
        var act = () => submitted.Close(new DateOnly(2026, 6, 1), false, Today);
        act.Should().Throw<DomainException>().Which.Code.Should().Be("account.close.notactive");

        var closed = MakeActive();
        closed.Close(new DateOnly(2026, 1, 1), false, Today);
        closed.ClearDomainEvents();
        var reClose = () => closed.Close(new DateOnly(2026, 1, 1), false, Today);
        reClose.Should().Throw<DomainException>().Which.Code.Should().Be("account.close.notactive");
        closed.DomainEvents.Should().BeEmpty();          // no second event
    }

    [Fact] // AC-5
    public void Close_future_date_throws_but_today_is_allowed()
    {
        MakeActive().Invoking(a => a.Close(Today.AddDays(1), false, Today))
            .Should().Throw<DomainException>().Which.Code.Should().Be("account.close.future");

        var ok = MakeActive();
        ok.Close(Today, false, Today);                   // closedOn == today accepted
        ok.Status.Should().Be(SavingsAccountStatus.Closed);
    }

    [Fact] // AC-6
    public void Close_before_activation_throws_but_activation_date_is_allowed()
    {
        MakeActive(activatedOn: new DateOnly(2026, 1, 1))
            .Invoking(a => a.Close(new DateOnly(2025, 12, 31), false, Today))
            .Should().Throw<DomainException>().Which.Code.Should().Be("account.close.beforeactivation");

        var ok = MakeActive(activatedOn: new DateOnly(2026, 1, 1));
        ok.Close(new DateOnly(2026, 1, 1), false, Today); // closedOn == activation accepted
        ok.Status.Should().Be(SavingsAccountStatus.Closed);
    }

    [Fact] // AC-7
    public void Close_before_last_transaction_throws_but_equal_is_allowed()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 3, 10), 1000m, Today);
        a.WithdrawMoney(new DateOnly(2026, 3, 10), 1000m, Today); // balance 0, last txn Mar 10
        a.ClearDomainEvents();

        a.Invoking(x => x.Close(new DateOnly(2026, 2, 1), false, Today))
            .Should().Throw<DomainException>().Which.Code.Should().Be("account.close.afterlasttransaction");

        a.Close(new DateOnly(2026, 3, 10), false, Today); // closedOn == lastTxnDate accepted
        a.Status.Should().Be(SavingsAccountStatus.Closed);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SavingsAccountCloseTests"`
Expected: FAIL (compile error: `Close` / `ClosedOn` / `SavingsAccountClosed` not defined)

- [ ] **Step 3: Implement** — add to `SavingsAccountEvents.cs`:

```csharp
public sealed record SavingsAccountClosed(Guid AccountId, DateOnly ClosedOn, decimal BalanceAfter) : IDomainEvent;
```

Add the property to `SavingsAccount.cs` (after `WithdrawnOn`, line 29):

```csharp
    public DateOnly? ClosedOn { get; private set; }
```

Add the method to `SavingsAccount.cs` (after `PostInterest`, before `EnsureTransactionAllowed`).
In this task the `withdrawBalance` branch is present but the sweep body lands in Task 2 — for now reject
a non-zero balance regardless of the flag (so AC-3 passes and Task 2 only relaxes the `true` branch):

```csharp
    public void Close(DateOnly closedOn, bool withdrawBalance, DateOnly today)
    {
        if (Status != SavingsAccountStatus.Active)
            throw new DomainException("account.close.notactive",
                $"Cannot close an account in {Status} status.");
        if (closedOn < ActivatedOn!.Value)
            throw new DomainException("account.close.beforeactivation",
                "Close date cannot be before the account's activation date.");
        if (closedOn > today)
            throw new DomainException("account.close.future",
                "Close date cannot be in the future.");
        if (_transactions.Count > 0)
        {
            var lastTransactionDate = _transactions.Max(t => t.TransactionDate);
            if (closedOn < lastTransactionDate)
                throw new DomainException("account.close.afterlasttransaction",
                    "Close date cannot be before the last transaction date.");
        }

        // Slice 2 (Task 2) fills in the pivot-exempt settle here:
        // if (withdrawBalance && AccountBalance > 0m) InsertWithdrawalUnchecked(closedOn, AccountBalance);

        if (AccountBalance != 0m)
            throw new DomainException("account.close.balance.nonzero",
                "Account balance must be zero to close. Sweep funds or pass withdrawBalance=true.");

        Status = SavingsAccountStatus.Closed;
        ClosedOn = closedOn;
        Raise(new SavingsAccountClosed(Id, closedOn, AccountBalance));
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SavingsAccountCloseTests"`
Expected: PASS (sweep tests AC-2/AC-8 are added in Task 2)

- [ ] **Step 5: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): close savings account (terminal transition + ClosedOn stamp)"
```

---

### Task 2: Domain — `withdrawBalance` pivot-exempt settle-to-zero

Slice 2 (Part 1 §7, §2.2): when `withdrawBalance && AccountBalance > 0`, sweep the full balance dated
`closedOn` via an **internal pivot-exempt** path that raises no `SavingsWithdrawn` event. Extract the
shared timeline mechanics so `WithdrawMoney` and the settle both use them (no duplication).

**Files:**
- Modify: `services/savings-accounts/CoreBanking.Accounts.Domain/SavingsAccount.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/SavingsAccountCloseTests.cs` (append)

- [ ] **Step 1: Write the failing tests** (append to `SavingsAccountCloseTests.cs`)

```csharp
    [Fact] // AC-2
    public void Close_with_sweep_settles_to_zero_records_withdrawal_and_raises_no_withdrawn_event()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 10), 1000m, Today);   // balance 1000, no other txns
        a.ClearDomainEvents();

        a.Close(new DateOnly(2026, 3, 15), withdrawBalance: true, Today);

        a.AccountBalance.Should().Be(0m);
        a.Status.Should().Be(SavingsAccountStatus.Closed);
        a.Transactions.Should().Contain(t =>
            t.Type == SavingsTransactionType.Withdrawal &&
            t.Amount == 1000m && t.TransactionDate == new DateOnly(2026, 3, 15));
        a.DomainEvents.OfType<SavingsWithdrawn>().Should().BeEmpty();        // no separate event
        a.DomainEvents.OfType<SavingsAccountClosed>().Should().ContainSingle()
            .Which.BalanceAfter.Should().Be(0m);
    }

    [Fact] // AC-8 — pivot exemption: post interest then close same day
    public void Close_with_sweep_on_pivot_date_succeeds()
    {
        var a = MakeActive();
        a.Deposit(new DateOnly(2026, 1, 1), 1000m, Today);
        a.PostInterest(new DateOnly(2026, 1, 31), Today);     // pivot = Jan 31, balance 1004.25
        a.InterestPostedTillDate.Should().Be(new DateOnly(2026, 1, 31));
        a.ClearDomainEvents();

        // closedOn == lastTxnDate (Jan 31) == pivot. Public WithdrawMoney would throw
        // account.transaction.beforepivot; the internal settle is pivot-exempt.
        a.Close(new DateOnly(2026, 1, 31), withdrawBalance: true, Today);

        a.AccountBalance.Should().Be(0m);
        a.Status.Should().Be(SavingsAccountStatus.Closed);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~SavingsAccountCloseTests"`
Expected: FAIL (the two new tests: balance not swept / pivot guard thrown)

- [ ] **Step 3: Implement** — in `SavingsAccount.cs`:

(a) Extract the shared mechanics. Replace the body of `WithdrawMoney` (lines 114–136) so it delegates,
and add the private helper:

```csharp
    public Guid WithdrawMoney(DateOnly on, decimal amount, DateOnly today)
    {
        EnsureTransactionAllowed(on, today);
        EnsurePositive(amount);
        var tx = InsertWithdrawalUnchecked(on, amount);
        Raise(new SavingsWithdrawn(Id, tx.Id, on, amount, AccountBalance));
        return tx.Id;
    }

    // Inserts a withdrawal candidate, asserts the full-timeline balance never goes
    // negative (Fineract validateAccountBalanceConstraints), commits it, and recomputes
    // running balances. Raises NO event and applies NO status/date/pivot guards —
    // callers (WithdrawMoney, the close-settle) decide which guards run first.
    private SavingsAccountTransaction InsertWithdrawalUnchecked(DateOnly on, decimal amount)
    {
        var candidate = SavingsAccountTransaction.Create(
            Id, SavingsTransactionType.Withdrawal, on, amount, NextTransactionSequence());
        decimal balance = 0m;
        foreach (var t in _transactions.Append(candidate)
                     .OrderBy(x => x.TransactionDate).ThenBy(x => x.Sequence))
        {
            balance += t.IsCredit ? t.Amount : -t.Amount;
            if (balance < 0m)
                throw new DomainException("account.balance.insufficient",
                    $"Insufficient balance for a withdrawal of {amount} on {on:yyyy-MM-dd}.");
        }
        _transactions.Add(candidate);
        RebuildRunningBalances();
        return candidate;
    }
```

(b) Enable the settle branch in `Close()` — replace the Slice-2 placeholder comment with:

```csharp
        if (withdrawBalance && AccountBalance > 0m)
            InsertWithdrawalUnchecked(closedOn, AccountBalance);   // pivot-exempt, no event
```

- [ ] **Step 4: Run the full unit test project** (verify `WithdrawMoney` tests from the prior slice still green after the refactor)

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests`
Expected: PASS (all — existing deposit/withdraw/interest tests + the new close tests)

- [ ] **Step 5: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): pivot-exempt settle-to-zero on close (withdrawBalance)"
```

---

### Task 3: Application + API + integration event + outbox

Slice 3 (Part 1 §7): the Mediator command/validator/handler, the DTO field, the EF mapping, the
integration event + outbox map case, and the `POST {id}/close` endpoint.

**Files:**
- Create: `services/savings-accounts/CoreBanking.Accounts.Application/Accounts/CloseSavingsAccount.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Application/Accounts/SavingsAccountDto.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/Configurations/SavingsAccountConfiguration.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/SavingsAccountReadRepository.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Events/SavingsAccountIntegrationEvents.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/DependencyInjection.cs`
- Modify: `services/savings-accounts/CoreBanking.Accounts.Api/Controllers/SavingsAccountsController.cs`
- Test: `services/savings-accounts/tests/CoreBanking.Accounts.UnitTests/Handlers/CloseSavingsAccountHandlerTests.cs` (create)

- [ ] **Step 1: Write the failing handler test**

```csharp
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests --filter "FullyQualifiedName~CloseSavingsAccountHandlerTests"`
Expected: FAIL (compile error: `CloseSavingsAccountCommand` not defined)

- [ ] **Step 3: Implement the command** — `CloseSavingsAccount.cs` (mirror `WithdrawFromSavingsAccount.cs`):

```csharp
using CoreBanking.Accounts.Application.Abstractions;
using CoreBanking.Accounts.Domain;
using CoreBanking.BuildingBlocks.Application;
using CoreBanking.BuildingBlocks.Domain;
using FluentValidation;
using Mediator;

namespace CoreBanking.Accounts.Application.Accounts;

public sealed record CloseSavingsAccountCommand(
    Guid AccountId,
    DateOnly ClosedOn,
    bool WithdrawBalance = false) : ICommand;

public sealed class CloseSavingsAccountValidator : AbstractValidator<CloseSavingsAccountCommand>
{
    public CloseSavingsAccountValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
    }
}

public sealed class CloseSavingsAccountHandler(
    ISavingsAccountRepository repo,
    ISavingsAccountUnitOfWork uow,
    IDateTimeProvider dateTime)
    : ICommandHandler<CloseSavingsAccountCommand>
{
    public async ValueTask<Unit> Handle(CloseSavingsAccountCommand cmd, CancellationToken ct)
    {
        var account = await repo.FindAsync(cmd.AccountId, ct)
            ?? throw new NotFoundException(nameof(SavingsAccount), cmd.AccountId);

        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);
        account.Close(cmd.ClosedOn, cmd.WithdrawBalance, today);

        await uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
```

- [ ] **Step 4: Extend the DTO + EF mapping + read projection**

In `SavingsAccountDto.cs`, add `ClosedOn` after `InterestPostedTillDate`:

```csharp
    DateOnly? InterestPostedTillDate,
    DateOnly? ClosedOn);
```

In `SavingsAccountConfiguration.cs`, add after the `WithdrawnOn` mapping (line 26):

```csharp
        b.Property(x => x.ClosedOn).HasColumnName("CLOSEDON");
```

In `SavingsAccountReadRepository.cs`, add `a.ClosedOn` to the DTO projection as the new last argument
(after `a.InterestPostedTillDate`).

- [ ] **Step 5: Implement integration event + outbox map**

Append to `SavingsAccountIntegrationEvents.cs`:

```csharp
public sealed record SavingsAccountClosedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    long Version,
    Guid AccountId,
    DateOnly ClosedOn,
    decimal BalanceAfter)
    : IntegrationEvent(EventId, OccurredOnUtc, Version)
{
    public override string Topic => "savings-accounts.events";
    public override string AggregateKey => AccountId.ToString();
}
```

In `DependencyInjection.cs`, add a case before `_ => null` in `DomainEventToIntegrationEventMap`:

```csharp
            SavingsAccountClosed e => new SavingsAccountClosedIntegrationEvent(
                Guid.CreateVersion7(), DateTimeOffset.UtcNow, 1,
                e.AccountId, e.ClosedOn, e.BalanceAfter),
```

- [ ] **Step 6: Add the API endpoint** — in `SavingsAccountsController.cs`, after the `PostInterest` action:

```csharp
    /// <summary>Close an active savings account.</summary>
    /// <remarks>
    /// Corresponds to Fineract <c>POST /v1/savingsaccounts/{accountId}?command=close</c>.
    /// Validates the close date, optionally sweeps the remaining balance to zero (dated the close
    /// date, pivot-exempt), then transitions the account to the terminal <c>Closed</c> (600) state.
    /// </remarks>
    /// <response code="204">Account closed.</response>
    /// <response code="404">Account not found.</response>
    /// <response code="422">
    /// Business rule violation: <c>account.close.notactive</c>, <c>account.close.beforeactivation</c>,
    /// <c>account.close.future</c>, <c>account.close.afterlasttransaction</c>,
    /// <c>account.close.balance.nonzero</c>.
    /// </response>
    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseAccountRequest body, CancellationToken ct)
    {
        await mediator.Send(new CloseSavingsAccountCommand(id, body.ClosedOn, body.WithdrawBalance), ct);
        return NoContent();
    }
```

And after the existing request records at the bottom of the file:

```csharp
/// <summary>Request body for the close-account operation.</summary>
/// <param name="ClosedOn">Close date — not in the future, not before activation or the last transaction.</param>
/// <param name="WithdrawBalance">When true, sweep any remaining balance to zero (dated <c>ClosedOn</c>) before closing.</param>
public sealed record CloseAccountRequest(DateOnly ClosedOn, bool WithdrawBalance = false);
```

- [ ] **Step 7: Build the whole service + run unit tests**

```bash
dotnet build services/savings-accounts/CoreBanking.Accounts.Api
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests
```
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add services/savings-accounts
git commit -m "feat(accounts): close savings account command, endpoint, and integration event"
```

---

### Task 4: Migration + integration test + verification

migration-dba generates the `CLOSEDON` migration; the integration test proves the Oracle round-trip of
`ClosedOn` + terminal status; final solution-wide verification.

**Files:**
- Create: `services/savings-accounts/CoreBanking.Accounts.Infrastructure/Persistence/Migrations/<timestamp>_AddSavingsAccountClosedOn.cs` (generated)
- Create: `services/savings-accounts/tests/CoreBanking.Accounts.IntegrationTests/SavingsAccountClosePersistenceTests.cs`

- [ ] **Step 1: Generate the migration** (migration-dba)

```bash
dotnet ef migrations add AddSavingsAccountClosedOn \
  --project services/savings-accounts/CoreBanking.Accounts.Infrastructure \
  --startup-project services/savings-accounts/CoreBanking.Accounts.Api \
  --context SavingsAccountsWriteDbContext
```
Expected: a migration whose `Up()` is a single `AddColumn` for `CLOSEDON` (nullable `DATE`), no
`DropColumn`/`DropTable`. Inspect before committing.

- [ ] **Step 2: Write the integration test (AC-10 round-trip)**

```csharp
using CoreBanking.Accounts.Domain;
using CoreBanking.Accounts.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.Oracle;
using Xunit;

namespace CoreBanking.Accounts.IntegrationTests;

public sealed class SavingsAccountClosePersistenceTests : IAsyncLifetime
{
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
    public async Task Close_with_sweep_persists_closed_status_and_closedon()
    {
        var today = new DateOnly(2026, 6, 7);
        Guid accountId;

        await using (var ctx = new SavingsAccountsWriteDbContext(Options))
        {
            await ctx.Database.MigrateAsync();

            var account = SavingsAccount.SubmitApplication(
                Guid.NewGuid(), Guid.NewGuid(), "SA-CLOSE-IT", "USD", 2, 5.0m, new DateOnly(2026, 1, 1));
            account.Approve(new DateOnly(2026, 1, 1));
            account.Activate(new DateOnly(2026, 1, 1));
            account.Deposit(new DateOnly(2026, 1, 10), 1000m, today);
            account.Close(new DateOnly(2026, 3, 15), withdrawBalance: true, today);
            account.ClearDomainEvents();   // outbox interceptor not wired in this raw context
            accountId = account.Id;

            ctx.SavingsAccounts.Add(account);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new SavingsAccountsWriteDbContext(Options))
        {
            var loaded = await ctx.SavingsAccounts
                .Include(a => a.Transactions)
                .SingleAsync(a => a.Id == accountId);

            loaded.Status.Should().Be(SavingsAccountStatus.Closed);
            loaded.ClosedOn.Should().Be(new DateOnly(2026, 3, 15));
            loaded.AccountBalance.Should().Be(0m);
            loaded.Transactions.Should().Contain(t =>
                t.Type == SavingsTransactionType.Withdrawal && t.Amount == 1000m);
        }
    }
}
```

- [ ] **Step 3: Run the integration test** (requires Docker running)

Run: `dotnet test services/savings-accounts/tests/CoreBanking.Accounts.IntegrationTests --filter "FullyQualifiedName~SavingsAccountClosePersistenceTests"`
Expected: PASS.

- [ ] **Step 4: Solution-wide verification**

```bash
dotnet build CoreBanking.slnx
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.ArchTests
dotnet test tests/CoreBanking.ContractTests
```
Expected: all PASS. ArchTests guard the dependency rule — `Close()` and its event live in Domain and
reference nothing outward; the command lives in Application; the integration event/outbox in
Infrastructure.

- [ ] **Step 5: Update docs** — update `IMPLEMENTATION_PLAN.md` (and the controller class `<remarks>`,
which already lists the lifecycle: add that `Active → Closed (600) via close` is now implemented, sweeps
optional, terminal) to record that the savings-accounts service implements Fineract-derived account
closure (Active→Closed, five close-date/balance validations, optional pivot-exempt sweep, `ClosedOn`,
`SavingsAccountClosed` integration event; post-interest-on-close deferred).

- [ ] **Step 6: Final commit**

```bash
git add -A
git commit -m "feat(accounts): persist savings account closure (migration + oracle round-trip)"
```

---

# Self-Review (performed at plan time)

- **Spec coverage:** every Part 1 rule maps to a task — domain close-date/balance rules + flip + stamp +
  event (T1: AC-1, AC-3, AC-4, AC-5, AC-6, AC-7, AC-9, AC-11); pivot-exempt sweep (T2: AC-2, AC-8);
  command/handler/validator + DTO + EF + integration event + outbox + endpoint (T3); migration + Oracle
  round-trip + solution verification (T4: AC-10). Deferred items (§2.3, §8) are explicitly *not*
  implemented and called out in §2.6.
- **Reconciliations recorded:** (1) sweep is an internal pivot-exempt path inside `Close()`, **not** a
  handler-orchestrated public `WithdrawMoney` — required by AC-2 (no second event), AC-8 (pivot), and
  §2.1 (validate-before-settle ordering); reuse is at the `InsertWithdrawalUnchecked` mechanics level.
  (2) event named `SavingsAccountClosed`/`SavingsAccountClosedIntegrationEvent` to match the lifecycle
  family, not the spec prose's "SavingsClosed".
- **Signature consistency:** `Close(DateOnly closedOn, bool withdrawBalance, DateOnly today)` used
  identically in domain (T1/T2), handler (T3), and tests (T1/T2/T3/T4). `CloseSavingsAccountCommand(Guid
  AccountId, DateOnly ClosedOn, bool WithdrawBalance = false)` consistent across handler, validator,
  controller, and tests.
- **Reuse, no reinvention:** `Close` reuses `RebuildRunningBalances`, `NextTransactionSequence`,
  `SavingsAccountTransaction.Create`, and the negative-balance replay (factored into
  `InsertWithdrawalUnchecked`, shared with `WithdrawMoney`). No new transaction type, no new table, no
  new topic, no duplicated timeline logic.
