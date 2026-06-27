# CoreBanking — GitHub Copilot Instructions

## What this is

A .NET 10 microservices platform re-implementing Apache Fineract's **savings-account** domain (creation/lifecycle + transactions + interest posting). Three autonomous services — **Clients**, **Savings Products**, **Savings Accounts** — sit behind a YARP gateway and communicate over Apache Kafka (event-driven reference replication + local read models). Each service is internally Clean Architecture + DDD + CQRS, owns its own Oracle schema, and runs its own EF Core migrations.

`docs/IMPLEMENTATION_PLAN.md` (design) and `docs/EXECUTION_PLAN.md` (build playbook) are the source-of-truth specs.

**`ARCHITECTURE.md` is the living map of the running system.** Update it in the same commit whenever you add, remove, or change a service — a new endpoint, a published/consumed integration event, a Kafka topic, a schema/table, a background consumer, or a gateway route.

---

## Commands

The solution file is `CoreBanking.slnx` (`.slnx` XML format — pass it explicitly).

```bash
# Build
dotnet build CoreBanking.slnx

# All tests
dotnet test CoreBanking.slnx

# Fast tests only (skips Docker-dependent integration tests)
dotnet test CoreBanking.slnx --filter "Category!=Integration"

# Single service test project
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests

# Single test by name
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests \
  --filter "FullyQualifiedName~SavingsAccountPostInterestTests"
```

Integration tests require a running Docker daemon (Testcontainers spins real Oracle + Kafka containers).

### Run locally

```bash
# From docker/ — gateway, 3 services, Kafka (KRaft) + kafka-ui, oracle-free
docker compose --profile free up

# Oracle primary + Active Data Guard standby (Enterprise Edition)
docker compose --profile dataguard up
```

Ports: gateway `5100`, clients `5101`, products `5102`, accounts `5103`, Kafka `9092`, kafka-ui `8080`, Oracle `1521`.

### EF Core migrations

Each service has separate Write and Read DbContexts; migrations live on the Write context.

```bash
# Example for Accounts
dotnet ef migrations add <Name> \
  --project services/savings-accounts/CoreBanking.Accounts.Infrastructure \
  --startup-project services/savings-accounts/CoreBanking.Accounts.Api \
  --context SavingsAccountsWriteDbContext
```

| Service          | Write context                      | Read context                      |
|------------------|------------------------------------|-----------------------------------|
| Savings Accounts | `SavingsAccountsWriteDbContext`    | `SavingsAccountsReadDbContext`    |
| Savings Products | `SavingsProductsWriteDbContext`    | `SavingsProductsReadDbContext`    |
| Clients          | `ClientsWriteDbContext`            | `ClientsReadDbContext`            |

---

## Architecture

### Directory layout & critical naming gotcha

Monorepo: `services/<service>/` (4 layer projects + `tests/`), `shared/` (BuildingBlocks), `gateway/`, `tests/` (cross-service contract tests).

**Directory names and C# namespaces diverge — always navigate by namespace:**

| Service          | Directory                   | Namespace prefix          |
|------------------|-----------------------------|---------------------------|
| Savings Accounts | `services/savings-accounts/`| `CoreBanking.Accounts.*`  |
| Savings Products | `services/savings-products/`| `CoreBanking.Products.*`  |
| Clients          | `services/clients/`         | `CoreBanking.Clients.*`   |

### Clean Architecture layers (enforced by ArchTests)

`Domain` → `Application` → `Infrastructure` → `Api`. Dependencies point **inward only**.

- **Domain** references nothing. The interest engine lives here (`Domain/Interest/`).
- **Application** references Domain only.
- **Infrastructure** references Application + Domain.
- **Api** references all layers.

Violations fail `*.ArchTests` (NetArchTest). Do not introduce outward dependencies.

`shared/CoreBanking.BuildingBlocks.*` (Domain / Application / Infrastructure / Messaging) hold **technical concerns only** — no business logic.

### CQRS + request pipeline

- Use **`martinothamar/Mediator`** (source-generated) — **NOT MediatR**. Handlers implement `ICommandHandler<,>` / `IQueryHandler<,>` and return `ValueTask`.
- Pipeline: **Logging → Validation** (→ UnitOfWork where wired). Validation via FluentValidation; failures → 400.
- Commands → Write context (primary). Queries → Read context (`NoTracking`, replica).
- API layer: **MVC controllers** in `Api/Controllers/`. Error mapping via `ExceptionToProblemDetailsHandler` (RFC 7807): validation→400, domain→422, not-found→404, concurrency→409.

### Event-driven integration

Each service owns its own Oracle schema (`CLIENTS` / `PRODUCTS` / `SAVINGS`) — **no cross-service foreign keys or cross-schema queries**.

1. Domain method raises a **domain event** → EF interceptor writes to `OUTBOX_MESSAGES` in the same transaction → background dispatcher publishes to Kafka.
2. Domain-event → integration-event mapping lives in `Infrastructure/DependencyInjection.cs` (`DomainEventToIntegrationEventMap` switch). **Adding a published event = 3 changes**: domain event (Domain) + integration event (`Infrastructure/Events/`) + new `case` in the map.
3. Accounts service consumes `clients.events` and `products.events`, dedupes via `INBOX_MESSAGES` (keyed by `eventId`), and upserts local read-model replicas `CLIENT_REF` / `PRODUCT_REF`. Account opening validates against these local copies only.
4. Reference topics are **log-compacted** and keyed by aggregate id — replay from offset 0 to rebuild a wiped read model.

Kafka topics: `clients.events`, `products.events`, `savings-accounts.events`.  
`tests/CoreBanking.ContractTests` asserts published schemas match consumer expectations.

### Domain conventions

- **Clock-free domain**: methods receive dates as parameters (e.g. `Deposit(on, amount, today)`); handlers inject `IDateTimeProvider`.
- **Fineract-faithful status codes**: account status Submitted=100, Approved=200, Active=300, Withdrawn=400, Rejected=500, Closed=600; transaction types deposit=1, withdrawal=2, interest-posting=3.
- **Money math**: `decimal` throughout. Interest compounds iteratively (no `Math.Pow`); rounds at posting time with `MidpointRounding.AwayFromZero`. Interest posting is **forward-only** — transactions on/before `InterestPostedTillDate` are rejected.
- Tests access `internal` members via `InternalsVisibleTo` declared in each Domain `.csproj`.

---

## Build & workflow conventions

- **.NET 10 SDK pinned** (`global.json`, `rollForward: latestFeature`).
- `Directory.Build.props`: `Nullable`, `ImplicitUsings`, **`TreatWarningsAsErrors=true`** — C# warnings break the build.
- Central package versions in `Directory.Packages.props`.
- Default branch is `master`. Feature work goes on `feature/<topic>` branches; merge with `--no-ff`.
