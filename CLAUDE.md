# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 10 microservices platform re-implementing Apache Fineract's **savings-account** domain (creation/lifecycle + transactions + interest posting). Three autonomous services — **Clients**, **Savings Products**, **Savings Accounts** — sit behind a YARP gateway and communicate over Apache Kafka (event-driven reference replication + local read models). Each service is internally Clean Architecture + DDD + CQRS, owns its own Oracle schema, and runs its own EF Core migrations.

`docs/IMPLEMENTATION_PLAN.md` (design) and `docs/EXECUTION_PLAN.md` (build playbook) are the source-of-truth specs and explain *why* things are shaped this way. Note both predate some reality: see the naming gotcha below.

**`ARCHITECTURE.md` is the living map of the running system** (services, endpoints, events, topics, schemas, diagrams as Mermaid). **You MUST update it in the same change whenever you add, remove, or change a service** — a new endpoint, a published/consumed integration event, a Kafka topic, a schema/table, a background consumer, or a gateway route. It ends with a change checklist; follow it. A stale architecture diagram is worse than none.

## Commands

All commands run from the repo root unless noted. The solution file is `CoreBanking.slnx` (note: `.slnx`, the XML solution format — pass it explicitly to `dotnet`).

```bash
# Build everything
dotnet build CoreBanking.slnx

# Run all tests
dotnet test CoreBanking.slnx

# Fast tests only — what CI runs (skips Docker-dependent suites)
dotnet test CoreBanking.slnx --filter "Category!=Integration"

# One test project (each service has UnitTests / ArchTests / IntegrationTests)
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests

# A single test or class by name
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests \
  --filter "FullyQualifiedName~SavingsAccountPostInterestTests"
```

**Integration tests require a running Docker daemon** — they use Testcontainers to spin real Oracle (`gvenzl/oracle-free`) and Kafka containers. Without Docker they fail at container construction (`Docker is either not running or misconfigured`), not in assertions. They are *not* currently tagged with a `Category=Integration` trait, so the CI `Category!=Integration` filter does not actually exclude them — it relies on the runner having Docker.

### Running the platform locally

```bash
# From docker/ — brings up gateway, 3 services, Kafka (KRaft) + kafka-ui, and one oracle-free holding all schemas
docker compose --profile free up

# Production-faithful variant: Oracle primary + Active Data Guard standby (Enterprise Edition)
docker compose --profile dataguard up
```

Ports: gateway `5100`, clients-svc `5101`, products-svc `5102`, accounts-svc `5103`, Kafka `9092`, kafka-ui `8080`, Oracle `1521`.

### EF Core migrations

Each service has separate **Write** and **Read** DbContexts; migrations live on the Write context. Example (Accounts):

```bash
dotnet ef migrations add <Name> \
  --project services/savings-accounts/CoreBanking.Accounts.Infrastructure \
  --startup-project services/savings-accounts/CoreBanking.Accounts.Api \
  --context SavingsAccountsWriteDbContext
```

Write/Read context names per service: `SavingsAccountsWriteDbContext`/`SavingsAccountsReadDbContext`, `ClientsWriteDbContext`/`ClientsReadDbContext`, `SavingsProductsWriteDbContext`/`SavingsProductsReadDbContext`.

## Architecture

### Layout & a critical naming gotcha

Monorepo: `services/<service>/` (4 layer projects + `tests/`), `shared/` (BuildingBlocks libs), `gateway/`, `tests/` (cross-service contract tests).

**Directory names and project namespaces diverge** — grep/navigate by the namespace, not the folder:

| Service | Directory | Project namespace |
|---|---|---|
| Savings Accounts | `services/savings-accounts/` | `CoreBanking.Accounts.*` |
| Savings Products | `services/savings-products/` | `CoreBanking.Products.*` |
| Clients | `services/clients/` | `CoreBanking.Clients.*` |

### Per-service Clean Architecture (dependency rule is enforced)

Four projects per service: `Domain` → `Application` → `Infrastructure` → `Api`. Dependencies point inward only: **Domain references nothing**, Application references Domain, Infrastructure references Application + Domain, Api references all. This is **enforced by `*.ArchTests` (NetArchTest)** — a violation fails the test suite. The interest engine lives in `Domain/Interest/` precisely because it must reference nothing.

`shared/CoreBanking.BuildingBlocks.*` (Domain / Application / Infrastructure / Messaging) hold **technical concerns only** — no business logic — so services stay autonomous: `Entity`/`AggregateRoot`/`IDomainEvent`, pipeline behaviors, audit & outbox interceptors, `IEventBus` + outbox/inbox machinery.

### CQRS + request pipeline

- **Mediator is `martinothamar/Mediator`** (source-generated, MIT) — **not MediatR**. Handlers implement `ICommandHandler<,>` / `IQueryHandler<,>` and return `ValueTask`.
- Pipeline behaviors run in order: **Logging → Validation** (and a UnitOfWork behavior where wired). Validation uses **FluentValidation**; failures become 400.
- Commands go through the Write context (primary); queries through the Read context (`NoTracking`, replica).
- API layer is **MVC controllers** (`Api/Controllers/`), migrated from the original Minimal API design. Errors map to RFC 7807 ProblemDetails via `ExceptionToProblemDetailsHandler`: validation→400, domain→422, not-found→404, concurrency(`Version` token)→409.

### Event-driven integration (the part that spans files)

Each service owns its Oracle schema (`CLIENTS` / `PRODUCTS` / `SAVINGS`) — **no cross-service foreign keys or cross-schema queries**. Services coordinate only through Kafka:

1. A domain method raises a **domain event**; an EF interceptor writes a row to `OUTBOX_MESSAGES` **in the same transaction** as the domain change. A background dispatcher publishes outbox rows to Kafka and marks them processed.
2. The **domain-event → integration-event mapping** lives in each Infrastructure project's `DependencyInjection.cs` (a `DomainEventToIntegrationEventMap` switch). **Adding a new published event is a three-part change**: define the domain event (Domain), define the integration event (Infrastructure `Events/`), and add a `case` to that map.
3. The Accounts service runs Kafka **consumers** (`Infrastructure/Consumers/`) for `clients.events` and `products.events`, deduping via the **inbox** (`INBOX_MESSAGES`, keyed by `eventId`) and upserting local read-model replicas `CLIENT_REF` / `PRODUCT_REF`. Opening an account validates against these local copies, not by calling other services.
4. Reference topics are **log-compacted** and keyed by aggregate id, so a wiped read model is rebuilt by replaying the topic from offset 0.

Topics: `clients.events`, `products.events`, `savings-accounts.events`. `tests/CoreBanking.ContractTests` asserts published event schemas match what consumers expect. The Clients and Products services expose their event contracts in dedicated `*.Contracts` projects; Accounts keeps its integration events in `Infrastructure/Events/`.

### Domain conventions

- **Domain is clock-free**: methods take `today`/dates as parameters (e.g. `Deposit(on, amount, today)`); handlers supply the value from `IDateTimeProvider`. This keeps the domain pure and testable.
- Status/enum codes stay **faithful to Fineract** (e.g. account status Submitted=100, Approved=200, Active=300, Withdrawn=400, Rejected=500, Closed=600; transaction types deposit=1, withdrawal=2, interest-posting=3).
- All money/interest math is in `decimal`; the savings interest engine compounds iteratively (no `Math.Pow` double round-trip) and rounds only at posting time with `MidpointRounding.AwayFromZero`. Interest posting is **forward-only** behind an `InterestPostedTillDate` pivot — transactions on/before the pivot are rejected (posted periods are immutable).
- Tests reach `internal` domain members via `InternalsVisibleTo` (declared in the Domain `.csproj`).

## Build & workflow conventions

- **`.NET 10` SDK is pinned** (`global.json`, rollForward `latestFeature`). `Directory.Build.props` sets `Nullable`, `ImplicitUsings`, and **`TreatWarningsAsErrors=true`** — C# warnings fail the build. (The MSB3277 EF `Relational` version-conflict warnings during build are benign and do not fail it.)
- Central package versions: `Directory.Packages.props`.
- `master` is the default branch. Do feature work on a branch and merge in (do not commit feature work straight to `master`); the prior workflow used `feature/<topic>` branches and `--no-ff` merges.
