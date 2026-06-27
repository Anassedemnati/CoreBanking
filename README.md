# CoreBanking

A production-grade **.NET 10 microservices platform** for core savings-account banking. Three autonomous services — **Clients**, **Savings Products**, **Savings Accounts** — sit behind a YARP API gateway and communicate asynchronously over **Apache Kafka**. Each service is built with Clean Architecture + DDD + CQRS, owns its own Oracle schema, and runs its own EF Core migrations.

---

## Table of Contents

- [Global Architecture](#global-architecture)
- [Services](#services)
- [Internal Service Architecture](#internal-service-architecture)
- [Event-Driven Integration](#event-driven-integration)
- [Savings Account Lifecycle](#savings-account-lifecycle)
- [Persistence](#persistence)
- [Deployment](#deployment)
- [Getting Started](#getting-started)
- [Testing](#testing)

---

## Global Architecture

```mermaid
flowchart TB
    Client(["API consumer"])

    subgraph Edge["Edge"]
        GW["Gateway · YARP reverse proxy\n:5100 → routes /api/v1/*"]
    end

    subgraph Services["Microservices (.NET 10 · Clean Architecture + CQRS)"]
        CSVC["Clients svc · :5101\nCorebanking.Clients.*"]
        PSVC["Savings Products svc · :5102\nCoreBanking.Products.*"]
        ASVC["Savings Accounts svc · :5103\nCoreBanking.Accounts.*"]
    end

    subgraph Kafka["Apache Kafka (KRaft, single broker :9092)"]
        TC["clients.events\n(log-compacted)"]
        TP["products.events\n(log-compacted)"]
        TA["savings-accounts.events"]
    end

    subgraph Oracle["Oracle — schema per service :1521"]
        SC[("CLIENTS schema")]
        SP[("PRODUCTS schema")]
        SA[("SAVINGS schema\n+ CLIENT_REF / PRODUCT_REF\nlocal read models")]
    end

    Client --> GW
    GW -->|/api/v1/clients/**| CSVC
    GW -->|/api/v1/savingsproducts/**| PSVC
    GW -->|/api/v1/savingsaccounts/**| ASVC

    CSVC --> SC
    PSVC --> SP
    ASVC --> SA

    CSVC -->|outbox → publish| TC
    PSVC -->|outbox → publish| TP
    ASVC -->|outbox → publish| TA
    TC -->|ClientsConsumer| ASVC
    TP -->|ProductsConsumer| ASVC

    classDef svc fill:#238636,stroke:#0b5d22,color:#fff
    classDef db fill:#9e6a03,stroke:#5c3e02,color:#fff
    classDef infra fill:#8957e5,stroke:#4c2889,color:#fff
    class CSVC,PSVC,ASVC svc
    class SC,SP,SA db
    class GW,TC,TP,TA infra
```

> **No synchronous service-to-service calls. No cross-schema queries.** Services share state only by publishing integration events. The Accounts service keeps local replicas (`CLIENT_REF`, `PRODUCT_REF`) populated by consuming the compacted reference topics.

---

## Services

| Service | Namespace | Port | Oracle Schema | Publishes | Consumes |
|---|---|---|---|---|---|
| **Clients** | `CoreBanking.Clients.*` | `:5101` | `CLIENTS` | `ClientRegistered`, `ClientActivated` → `clients.events` | — |
| **Savings Products** | `CoreBanking.Products.*` | `:5102` | `PRODUCTS` | `SavingsProductCreated` → `products.events` | — |
| **Savings Accounts** | `CoreBanking.Accounts.*` | `:5103` | `SAVINGS` | `SavingsAccountSubmitted/Approved/Activated/Rejected/Withdrawn/Closed`, `SavingsDeposited`, `SavingsWithdrawn`, `SavingsInterestPosted` → `savings-accounts.events` | `clients.events`, `products.events` |
| **Gateway** | `CoreBanking.Gateway` | `:5100` | — | — | — |

### API Endpoints

| Method & Route | Service | Action |
|---|---|---|
| `POST /api/v1/clients` | Clients | Register a client |
| `POST /api/v1/clients/{id}/activate` | Clients | Activate client |
| `GET  /api/v1/clients/{id}` | Clients | Get by id |
| `POST /api/v1/savingsproducts` | Products | Create product |
| `GET  /api/v1/savingsproducts/{id}` | Products | Get by id |
| `GET  /api/v1/savingsproducts` | Products | List all products |
| `POST /api/v1/savingsaccounts` | Accounts | Submit account application |
| `POST /api/v1/savingsaccounts/{id}/approve` | Accounts | Approve application |
| `POST /api/v1/savingsaccounts/{id}/activate` | Accounts | Activate account |
| `POST /api/v1/savingsaccounts/{id}/reject` | Accounts | Reject application |
| `POST /api/v1/savingsaccounts/{id}/withdraw` | Accounts | Withdraw application |
| `POST /api/v1/savingsaccounts/{id}/close` | Accounts | Close account |
| `POST /api/v1/savingsaccounts/{id}/transactions/deposit` | Accounts | Deposit money |
| `POST /api/v1/savingsaccounts/{id}/transactions/withdraw` | Accounts | Withdraw money |
| `POST /api/v1/savingsaccounts/{id}/postinterest` | Accounts | Post accrued interest (idempotent) |
| `GET  /api/v1/savingsaccounts/{id}/transactions` | Accounts | List transactions |
| `GET  /api/v1/savingsaccounts/{id}` | Accounts | Get account by id |

---

## Internal Service Architecture

Every service shares the same four-project Clean Architecture shape. Dependencies point **inward only** — enforced at build time by `*.ArchTests` (NetArchTest).

```mermaid
flowchart TD
    subgraph Svc["Service (e.g. Savings Accounts)"]
        API["Api\nMVC Controllers · OpenAPI\nProblemDetails handler · DI wiring"]
        APP["Application\nMediator commands & queries\nFluentValidation · pipeline behaviors\nport interfaces · read models"]
        INFRA["Infrastructure\nEF Core 10 + Oracle\nWrite/Read DbContext · repositories\nintegration events · Kafka consumers\nOutbox dispatcher · Inbox deduplication"]
        DOM["Domain\naggregates · value objects\ndomain events · invariants\nInterest/ (pure interest engine)"]

        API --> APP
        API --> INFRA
        INFRA --> APP
        APP --> DOM
        INFRA --> DOM
    end

    BB["shared/CoreBanking.BuildingBlocks.*\n(technical concerns only — no business logic)\nEntity · AggregateRoot · IDomainEvent\npipeline behaviors · audit interceptors\nIEventBus · OutboxMessage · InboxMessage · Kafka"]

    APP -.->|references| BB
    INFRA -.->|references| BB
    API -.->|references| BB

    classDef core fill:#1f6feb,stroke:#0b3d91,color:#fff
    classDef app fill:#238636,stroke:#0b5d22,color:#fff
    classDef infra fill:#9e6a03,stroke:#5c3e02,color:#fff
    classDef api fill:#8957e5,stroke:#4c2889,color:#fff
    classDef bb fill:#444c56,stroke:#22272e,color:#fff
    class DOM core
    class APP app
    class INFRA infra
    class API api
    class BB bb
```

**Dependency rule:** Domain → nothing · Application → Domain · Infrastructure → Application + Domain · Api → all.

**Request pipeline:** `LoggingBehavior` → `ValidationBehavior` (FluentValidation; failures → HTTP 400). Commands use the **Write** DbContext (primary, change-tracked); queries use the **Read** DbContext (replica, `NoTracking`). Errors map to RFC 7807 ProblemDetails: `validation→400`, `domain→422`, `not-found→404`, `concurrency→409`.

**Mediator:** [`martinothamar/Mediator`](https://github.com/martinothamar/Mediator) (source-generated) — **not MediatR**. Handlers implement `ICommandHandler<,>` / `IQueryHandler<,>` and return `ValueTask`.

---

## Event-Driven Integration

### Transactional outbox → Kafka → idempotent inbox

```mermaid
sequenceDiagram
    autonumber
    participant Cmd as Command handler (e.g. Clients)
    participant DBc as Service schema (e.g. CLIENTS)
    participant Disp as Outbox dispatcher (background)
    participant K as Kafka topic
    participant Con as Accounts consumer (background)
    participant DBa as SAVINGS schema

    Note over Cmd,DBc: Domain change + outbox row in ONE transaction
    Cmd->>DBc: UPDATE aggregate + raise domain event
    Cmd->>DBc: EF interceptor serializes → OUTBOX_MESSAGES
    Disp->>DBc: poll unprocessed rows
    Disp->>K: publish (key = aggregate id)
    Disp->>DBc: stamp ProcessedOnUtc

    Note over Con,DBa: Consume — dedupe then upsert local replica
    K-->>Con: deliver event
    Con->>DBa: INBOX_MESSAGES dedupe by EventId
    alt new event
        Con->>DBa: UPSERT CLIENT_REF or PRODUCT_REF
    else duplicate
        Con-->>Con: skip (idempotent)
    end
```

### Account opening — local replica validation

```mermaid
sequenceDiagram
    autonumber
    participant U as Caller
    participant A as SubmitSavingsApplication handler
    participant DB as SAVINGS schema

    U->>A: POST /api/v1/savingsaccounts
    A->>DB: load CLIENT_REF (exists & active?) + PRODUCT_REF (exists?)
    alt reference missing or client inactive
        A-->>U: 422 Unprocessable Entity
    else valid
        A->>DB: INSERT SAVINGS_ACCOUNTS (status=100) + OUTBOX row (one tx)
        A-->>U: 201 Created
    end
```

**Key facts:**
- Adding a new published event is a **3-part change**: raise domain event (Domain) → declare integration event (Infrastructure `Events/`) → add `case` to `DomainEventToIntegrationEventMap` in `Infrastructure/DependencyInjection.cs`.
- `clients.events` and `products.events` are **log-compacted** and keyed by entity id — replay from offset 0 to rebuild a wiped read model with no republish needed.
- `savings-accounts.events` is a normal (non-compacted) event stream.

---

## Savings Account Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Submitted : SubmitApplication()
    Submitted --> Approved  : Approve()
    Submitted --> Rejected  : Reject()
    Submitted --> Withdrawn : Withdraw()
    Approved  --> Active    : Activate()
    Active    --> Active    : Deposit() / WithdrawMoney() / PostInterest()
    Active    --> Closed    : Close()
    Rejected  --> [*]
    Withdrawn --> [*]
    Closed    --> [*]

    note right of Submitted : status 100
    note right of Approved  : status 200
    note right of Active    : status 300 — only state that accepts transactions
    note right of Withdrawn : status 400
    note right of Rejected  : status 500
    note right of Closed    : status 600 — terminal
```

- **Deposit** (type 1), **Withdraw** (type 2), **PostInterest** (type 3) only while `Active`.
- Withdrawals are validated against the full transaction timeline — balance may never go negative at any point, including backdated entries.
- Interest uses the **daily-balance method** with configurable compounding (daily/monthly), posting periods (monthly/quarterly/biannual/annual), and 360/365 day-count. All `decimal` arithmetic — no `Math.Pow`.
- Interest posting is **forward-only**: transactions on/before `InterestPostedTillDate` are rejected. Re-posting for the same date is idempotent.
- Closing requires zero balance; an optional `withdrawBalance=true` flag settles the balance first before marking the account closed.

---

## Persistence

```mermaid
erDiagram
    CLIENT_REF  ||--o{ SAVINGS_ACCOUNTS             : "validated by (local replica)"
    PRODUCT_REF ||--o{ SAVINGS_ACCOUNTS             : "templated by (local replica)"
    SAVINGS_ACCOUNTS ||--o{ SAVINGS_ACCOUNT_TRANSACTIONS : "transaction timeline"
```

| Schema | Tables |
|---|---|
| `CLIENTS` | `CLIENTS`, `OUTBOX_MESSAGES` |
| `PRODUCTS` | `SAVINGS_PRODUCTS`, `OUTBOX_MESSAGES` |
| `SAVINGS` | `SAVINGS_ACCOUNTS`, `SAVINGS_ACCOUNT_TRANSACTIONS`, `CLIENT_REF`, `PRODUCT_REF`, `INBOX_MESSAGES`, `OUTBOX_MESSAGES` |

**Oracle conventions:** `NUMBER(19,6)` for money/rates · `RAW(16)` for GUID keys (sequential v7) · `NUMBER` for enums · optimistic concurrency via a `Version` row token.

Each service has a **Write DbContext** (primary, owns migrations) and a **Read DbContext** (replica, `NoTracking`):

| Service | Write context | Read context |
|---|---|---|
| Savings Accounts | `SavingsAccountsWriteDbContext` | `SavingsAccountsReadDbContext` |
| Savings Products | `SavingsProductsWriteDbContext` | `SavingsProductsReadDbContext` |
| Clients | `ClientsWriteDbContext` | `ClientsReadDbContext` |

---

## Deployment

```mermaid
flowchart TB
    GW["Gateway :5100"]
    GW --> CSVC["clients-svc :5101"] & PSVC["products-svc :5102"] & ASVC["accounts-svc :5103"]
    CSVC & PSVC & ASVC <--> K{{"Kafka KRaft :9092"}}
    KUI["kafka-ui :8080"] -.-> K
    KINIT["kafka-init\n(creates topics, marks reference topics compacted)"] -.-> K

    subgraph free["profile: free  —  dev / CI"]
        OF[("oracle-free :1521\nCLIENTS / PRODUCTS / SAVINGS\nread == write")]
    end
    subgraph dataguard["profile: dataguard  —  prod-faithful"]
        OP[("oracle-primary EE")]
        OS[("oracle-standby\nActive Data Guard · read-only")]
        OP -. redo apply .-> OS
    end
    CSVC & PSVC & ASVC --> OF

    classDef db fill:#9e6a03,stroke:#5c3e02,color:#fff
    classDef infra fill:#8957e5,stroke:#4c2889,color:#fff
    class OF,OP,OS db
    class GW,K,KUI,KINIT infra
```

- **`free` profile** (default for dev/CI): single `gvenzl/oracle-free` holds all schemas; read == write. Fast startup.
- **`dataguard` profile**: Oracle Enterprise primary + Active Data Guard standby; each service's Replica connection points at the standby.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for local stack & integration tests)

### Run the full stack

```bash
cd docker
docker compose --profile free up
```

| Service | URL |
|---|---|
| Gateway | http://localhost:5100 |
| Clients svc | http://localhost:5101 |
| Savings Products svc | http://localhost:5102 |
| Savings Accounts svc | http://localhost:5103 |
| kafka-ui | http://localhost:8080 |
| Oracle | localhost:1521 |

### Build

```bash
dotnet build CoreBanking.slnx
```

### EF Core migrations

```bash
# Example — Savings Accounts service
dotnet ef migrations add <MigrationName> \
  --project services/savings-accounts/CoreBanking.Accounts.Infrastructure \
  --startup-project services/savings-accounts/CoreBanking.Accounts.Api \
  --context SavingsAccountsWriteDbContext
```

---

## Testing

| Layer | Projects | Covers |
|---|---|---|
| Unit | `*.UnitTests` per service + `BuildingBlocks.UnitTests` | Domain invariants, interest math, handlers, consumers (NSubstitute mocks) |
| Architecture | `*.ArchTests` per service | Inward dependency rule (NetArchTest) |
| Integration | `*.IntegrationTests` per service + Gateway | Testcontainers Oracle + Kafka: migrations, command→DB→query round-trip, outbox/inbox |
| Contract | `tests/CoreBanking.ContractTests` | Published event schemas match consumer expectations |

```bash
# All tests
dotnet test CoreBanking.slnx

# Fast tests (no Docker required)
dotnet test CoreBanking.slnx --filter "Category!=Integration"

# Single test project
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests

# Single test by name
dotnet test services/savings-accounts/tests/CoreBanking.Accounts.UnitTests \
  --filter "FullyQualifiedName~SavingsAccountPostInterestTests"
```

> Integration tests require a running Docker daemon — Testcontainers spins real Oracle and Kafka containers automatically.

---

## Project Structure

```
CoreBanking/
├── services/
│   ├── clients/                  # Clients service (CoreBanking.Clients.*)
│   ├── savings-products/         # Savings Products service (CoreBanking.Products.*)
│   └── savings-accounts/         # Savings Accounts service (CoreBanking.Accounts.*)
│       ├── CoreBanking.Accounts.Domain/
│       ├── CoreBanking.Accounts.Application/
│       ├── CoreBanking.Accounts.Infrastructure/
│       ├── CoreBanking.Accounts.Api/
│       └── tests/
├── shared/
│   └── CoreBanking.BuildingBlocks.*  # Technical concerns only (no business logic)
├── gateway/
│   └── CoreBanking.Gateway/          # YARP reverse proxy
├── tests/
│   └── CoreBanking.ContractTests/    # Cross-service contract tests
├── docker/
│   └── docker-compose.yml
├── docs/
│   ├── IMPLEMENTATION_PLAN.md
│   └── EXECUTION_PLAN.md
├── ARCHITECTURE.md                   # Living system map — keep it current
└── CoreBanking.slnx
```

> **Directory names and C# namespaces diverge.** Always navigate by namespace: `services/savings-accounts/` → `CoreBanking.Accounts.*`, `services/savings-products/` → `CoreBanking.Products.*`.
