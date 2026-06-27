# Graph Report - /Users/mac/Documents/Projects/CoreBanking  (2026-06-07)

## Corpus Check
- Corpus is ~38,396 words - fits in a single context window. You may not need a graph.

## Summary
- 584 nodes · 588 edges · 77 communities detected
- Extraction: 97% EXTRACTED · 3% INFERRED · 0% AMBIGUOUS · INFERRED: 19 edges (avg confidence: 0.86)
- Token cost: 85,000 input · 10,229 output

## Community Hubs (Navigation)
- [[_COMMUNITY_Architecture Plan & Deployment|Architecture Plan & Deployment]]
- [[_COMMUNITY_Account Lifecycle Commands (CQRS)|Account Lifecycle Commands (CQRS)]]
- [[_COMMUNITY_SavingsAccount Aggregate|SavingsAccount Aggregate]]
- [[_COMMUNITY_ReadWrite DbContexts|Read/Write DbContexts]]
- [[_COMMUNITY_Account Lifecycle Unit Tests|Account Lifecycle Unit Tests]]
- [[_COMMUNITY_Kafka Consumers & Gateway Middleware|Kafka Consumers & Gateway Middleware]]
- [[_COMMUNITY_REST API Controllers|REST API Controllers]]
- [[_COMMUNITY_EF Entity Configurations|EF Entity Configurations]]
- [[_COMMUNITY_EF Migrations|EF Migrations]]
- [[_COMMUNITY_Query Handlers|Query Handlers]]
- [[_COMMUNITY_EF Model Snapshots|EF Model Snapshots]]
- [[_COMMUNITY_Kafka Round-Trip Tests|Kafka Round-Trip Tests]]
- [[_COMMUNITY_Design-Time DbContext Factories|Design-Time DbContext Factories]]
- [[_COMMUNITY_SavingsProduct Unit Tests|SavingsProduct Unit Tests]]
- [[_COMMUNITY_Clients Consumer Tests|Clients Consumer Tests]]
- [[_COMMUNITY_Kafka Event Bus|Kafka Event Bus]]
- [[_COMMUNITY_EF Interceptors (OutboxAudit)|EF Interceptors (Outbox/Audit)]]
- [[_COMMUNITY_Exception Types|Exception Types]]
- [[_COMMUNITY_Mediator Pipeline Behaviors|Mediator Pipeline Behaviors]]
- [[_COMMUNITY_Products Consumer Tests|Products Consumer Tests]]
- [[_COMMUNITY_ClientTests|ClientTests]]
- [[_COMMUNITY_CreateSavingsProductHandlerTests|CreateSavingsProductHandlerTests]]
- [[_COMMUNITY_GatewayRoutingTests|GatewayRoutingTests]]
- [[_COMMUNITY_IntegrationEventContractTests|IntegrationEventContractTests]]
- [[_COMMUNITY_RegisterClientHandlerTests|RegisterClientHandlerTests]]
- [[_COMMUNITY_ExceptionTests|ExceptionTests]]
- [[_COMMUNITY_InboxService|InboxService]]
- [[_COMMUNITY_ProductRefRepository|ProductRefRepository]]
- [[_COMMUNITY_SavingsAccountRepository|SavingsAccountRepository]]
- [[_COMMUNITY_ClientRefRepository|ClientRefRepository]]
- [[_COMMUNITY_SystemCurrentUser|SystemCurrentUser]]
- [[_COMMUNITY_UtcDateTimeProvider|UtcDateTimeProvider]]
- [[_COMMUNITY_ClientRepository|ClientRepository]]
- [[_COMMUNITY_SavingsProductReadRepository|SavingsProductReadRepository]]
- [[_COMMUNITY_SavingsProductRepository|SavingsProductRepository]]
- [[_COMMUNITY_IInboxService|IInboxService]]
- [[_COMMUNITY_ExceptionToProblemDetailsHandler|ExceptionToProblemDetailsHandler]]
- [[_COMMUNITY_Entity|Entity]]
- [[_COMMUNITY_IEventBus|IEventBus]]
- [[_COMMUNITY_IClientRefRepository|IClientRefRepository]]
- [[_COMMUNITY_ISavingsAccountRepository|ISavingsAccountRepository]]
- [[_COMMUNITY_IProductRefRepository|IProductRefRepository]]
- [[_COMMUNITY_ArchitectureTests|ArchitectureTests]]
- [[_COMMUNITY_DependencyInjection|DependencyInjection]]
- [[_COMMUNITY_SavingsAccountUnitOfWork|SavingsAccountUnitOfWork]]
- [[_COMMUNITY_SavingsAccountReadRepository|SavingsAccountReadRepository]]
- [[_COMMUNITY_InitialSavings|InitialSavings]]
- [[_COMMUNITY_ArchitectureTests|ArchitectureTests]]
- [[_COMMUNITY_DependencyInjection|DependencyInjection]]
- [[_COMMUNITY_ClientReadRepository|ClientReadRepository]]
- [[_COMMUNITY_UnitOfWork|UnitOfWork]]
- [[_COMMUNITY_InitialClients|InitialClients]]
- [[_COMMUNITY_IClientRepository|IClientRepository]]
- [[_COMMUNITY_ArchitectureTests|ArchitectureTests]]
- [[_COMMUNITY_ISavingsProductRepository|ISavingsProductRepository]]
- [[_COMMUNITY_ISavingsProductReadRepository|ISavingsProductReadRepository]]
- [[_COMMUNITY_DependencyInjection|DependencyInjection]]
- [[_COMMUNITY_ProductUnitOfWork|ProductUnitOfWork]]
- [[_COMMUNITY_InitialProducts|InitialProducts]]
- [[_COMMUNITY_IOutboxDbContext|IOutboxDbContext]]
- [[_COMMUNITY_ISavingsAccountReadRepository|ISavingsAccountReadRepository]]
- [[_COMMUNITY_ISavingsAccountUnitOfWork|ISavingsAccountUnitOfWork]]
- [[_COMMUNITY_IClientReadRepository|IClientReadRepository]]
- [[_COMMUNITY_IUnitOfWork|IUnitOfWork]]
- [[_COMMUNITY_IProductUnitOfWork|IProductUnitOfWork]]
- [[_COMMUNITY_OutboxProcessorOptions|OutboxProcessorOptions]]
- [[_COMMUNITY_IAuditable|IAuditable]]
- [[_COMMUNITY_IDomainEvent|IDomainEvent]]
- [[_COMMUNITY_IDateTimeProvider|IDateTimeProvider]]
- [[_COMMUNITY_ICurrentUser|ICurrentUser]]
- [[_COMMUNITY_OutboxMessage|OutboxMessage]]
- [[_COMMUNITY_InboxMessage|InboxMessage]]
- [[_COMMUNITY_KafkaOptions|KafkaOptions]]
- [[_COMMUNITY_ClientRef|ClientRef]]
- [[_COMMUNITY_ProductRef|ProductRef]]
- [[_COMMUNITY_Program|Program]]
- [[_COMMUNITY_EF Core 10|EF Core 10]]

## God Nodes (most connected - your core abstractions)
1. `SavingsAccountTests` - 13 edges
2. `Savings Accounts Service` - 13 edges
3. `KafkaRoundTripTests` - 9 edges
4. `SubmitSavingsApplicationHandlerTests` - 9 edges
5. `SavingsAccount` - 9 edges
6. `SavingsProductTests` - 9 edges
7. `Clients Service` - 9 edges
8. `Apache Kafka (KRaft)` - 9 edges
9. `SavingsAccountsController` - 8 edges
10. `ClientsConsumerTests` - 8 edges

## Surprising Connections (you probably didn't know these)
- `CoreBanking Execution Plan` --references--> `CoreBanking Savings Account Platform`  [EXTRACTED]
  EXECUTION_PLAN.md → IMPLEMENTATION_PLAN.md
- `clients-svc service (compose)` --implements--> `Clients Service`  [INFERRED]
  docker/docker-compose.yml → IMPLEMENTATION_PLAN.md
- `products-svc service (compose)` --implements--> `Savings Products Service`  [INFERRED]
  docker/docker-compose.yml → IMPLEMENTATION_PLAN.md
- `accounts-svc service (compose)` --implements--> `Savings Accounts Service`  [INFERRED]
  docker/docker-compose.yml → IMPLEMENTATION_PLAN.md
- `Phase 0 — Repository & BuildingBlocks` --implements--> `BuildingBlocks Shared Libraries`  [EXTRACTED]
  EXECUTION_PLAN.md → IMPLEMENTATION_PLAN.md

## Hyperedges (group relationships)
- **Three autonomous services behind YARP gateway** — implementation_plan_clients_service, implementation_plan_savings_products_service, implementation_plan_savings_accounts_service, implementation_plan_api_gateway_yarp [EXTRACTED 1.00]
- **Event flow: outbox to Kafka to inbox read-model upsert** — implementation_plan_transactional_outbox, implementation_plan_apache_kafka, implementation_plan_inbox_idempotency, implementation_plan_local_read_models [EXTRACTED 1.00]
- **Per-service Clean Architecture layering (DDD + CQRS)** — implementation_plan_clean_architecture, implementation_plan_ddd, implementation_plan_cqrs, implementation_plan_dependency_rule [EXTRACTED 1.00]

## Communities

### Community 0 - "Architecture Plan & Deployment"
Cohesion: 0.06
Nodes (58): accounts-svc service (compose), clients-svc service (compose), gateway service (compose), kafka service (compose), kafka-init service (compose), kafka-ui service (compose), oracle-free service (compose), oracle-primary service (compose) (+50 more)

### Community 1 - "Account Lifecycle Commands (CQRS)"
Cohesion: 0.06
Nodes (16): AbstractValidator, ActivateSavingsAccountHandler, ApproveSavingsAccountHandler, RejectSavingsAccountHandler, SubmitSavingsApplicationHandler, SubmitSavingsApplicationValidator, WithdrawSavingsApplicationHandler, ActivateClientHandler (+8 more)

### Community 2 - "SavingsAccount Aggregate"
Cohesion: 0.1
Nodes (10): AggregateRoot, SavingsAccount, AggregateRoot, AggregateRootTests, Sample, Client, SavingsProduct, Entity (+2 more)

### Community 3 - "Read/Write DbContexts"
Cohesion: 0.09
Nodes (8): DbContext, IOutboxDbContext, ClientsReadDbContext, ClientsWriteDbContext, SavingsAccountsReadDbContext, SavingsAccountsWriteDbContext, SavingsProductsReadDbContext, SavingsProductsWriteDbContext

### Community 4 - "Account Lifecycle Unit Tests"
Cohesion: 0.15
Nodes (4): SavingsAccountTests, SubmitSavingsApplicationHandlerTests, DateOnly, Guid

### Community 5 - "Kafka Consumers & Gateway Middleware"
Cohesion: 0.11
Nodes (8): BackgroundService, ClientsConsumer, ProductsConsumer, KafkaConsumerBackgroundService, CorrelationIdMiddleware, OutboxProcessor, OutboxProcessorOptions, string

### Community 6 - "REST API Controllers"
Cohesion: 0.11
Nodes (4): ControllerBase, ClientsController, SavingsAccountsController, SavingsProductsController

### Community 7 - "EF Entity Configurations"
Cohesion: 0.12
Nodes (6): ClientConfiguration, ClientRefConfiguration, ProductRefConfiguration, SavingsAccountConfiguration, SavingsProductConfiguration, IEntityTypeConfiguration

### Community 8 - "EF Migrations"
Cohesion: 0.12
Nodes (7): Migration, CoreBanking.Clients.Infrastructure.Persistence.Migrations, InitialClients, CoreBanking.Products.Infrastructure.Persistence.Migrations, InitialProducts, CoreBanking.Accounts.Infrastructure.Persistence.Migrations, InitialSavings

### Community 9 - "Query Handlers"
Cohesion: 0.15
Nodes (5): GetSavingsAccountByIdHandler, GetClientByIdHandler, IQueryHandler, GetSavingsProductByIdHandler, ListSavingsProductsHandler

### Community 10 - "EF Model Snapshots"
Cohesion: 0.15
Nodes (7): ClientsWriteDbContextModelSnapshot, CoreBanking.Clients.Infrastructure.Persistence.Migrations, CoreBanking.Accounts.Infrastructure.Persistence.Migrations, SavingsAccountsWriteDbContextModelSnapshot, CoreBanking.Products.Infrastructure.Persistence.Migrations, SavingsProductsWriteDbContextModelSnapshot, ModelSnapshot

### Community 11 - "Kafka Round-Trip Tests"
Cohesion: 0.29
Nodes (3): KafkaRoundTripTests, IAsyncLifetime, KafkaContainer

### Community 12 - "Design-Time DbContext Factories"
Cohesion: 0.2
Nodes (4): IDesignTimeDbContextFactory, ClientsWriteDbContextFactory, SavingsAccountsWriteDbContextFactory, SavingsProductsWriteDbContextFactory

### Community 13 - "SavingsProduct Unit Tests"
Cohesion: 0.27
Nodes (1): SavingsProductTests

### Community 14 - "Clients Consumer Tests"
Cohesion: 0.36
Nodes (1): ClientsConsumerTests

### Community 15 - "Kafka Event Bus"
Cohesion: 0.25
Nodes (4): IDisposable, IEventBus, IProducer, KafkaEventBus

### Community 16 - "EF Interceptors (Outbox/Audit)"
Cohesion: 0.29
Nodes (3): AuditableEntityInterceptor, ConvertDomainEventsToOutboxInterceptor, SaveChangesInterceptor

### Community 17 - "Exception Types"
Cohesion: 0.29
Nodes (4): Exception, DomainException, NotFoundException, ValidationException

### Community 18 - "Mediator Pipeline Behaviors"
Cohesion: 0.29
Nodes (3): LoggingBehavior, ValidationBehavior, IPipelineBehavior

### Community 19 - "Products Consumer Tests"
Cohesion: 0.43
Nodes (1): ProductsConsumerTests

### Community 20 - "ClientTests"
Cohesion: 0.29
Nodes (1): ClientTests

### Community 21 - "CreateSavingsProductHandlerTests"
Cohesion: 0.33
Nodes (1): CreateSavingsProductHandlerTests

### Community 22 - "GatewayRoutingTests"
Cohesion: 0.29
Nodes (3): GatewayRoutingTests, HttpClient, IClassFixture

### Community 23 - "IntegrationEventContractTests"
Cohesion: 0.33
Nodes (2): IntegrationEventContractTests, JsonSerializerOptions

### Community 24 - "RegisterClientHandlerTests"
Cohesion: 0.33
Nodes (1): RegisterClientHandlerTests

### Community 25 - "ExceptionTests"
Cohesion: 0.4
Nodes (1): ExceptionTests

### Community 26 - "InboxService"
Cohesion: 0.4
Nodes (2): IInboxService, InboxService

### Community 27 - "ProductRefRepository"
Cohesion: 0.5
Nodes (2): IProductRefRepository, ProductRefRepository

### Community 28 - "SavingsAccountRepository"
Cohesion: 0.4
Nodes (2): ISavingsAccountRepository, SavingsAccountRepository

### Community 29 - "ClientRefRepository"
Cohesion: 0.5
Nodes (2): IClientRefRepository, ClientRefRepository

### Community 30 - "SystemCurrentUser"
Cohesion: 0.4
Nodes (2): ICurrentUser, SystemCurrentUser

### Community 31 - "UtcDateTimeProvider"
Cohesion: 0.4
Nodes (2): IDateTimeProvider, UtcDateTimeProvider

### Community 32 - "ClientRepository"
Cohesion: 0.4
Nodes (2): IClientRepository, ClientRepository

### Community 33 - "SavingsProductReadRepository"
Cohesion: 0.4
Nodes (2): ISavingsProductReadRepository, SavingsProductReadRepository

### Community 34 - "SavingsProductRepository"
Cohesion: 0.4
Nodes (2): ISavingsProductRepository, SavingsProductRepository

### Community 35 - "IInboxService"
Cohesion: 0.5
Nodes (1): IInboxService

### Community 36 - "ExceptionToProblemDetailsHandler"
Cohesion: 0.5
Nodes (2): ExceptionToProblemDetailsHandler, IExceptionHandler

### Community 37 - "Entity"
Cohesion: 0.5
Nodes (1): Entity

### Community 38 - "IEventBus"
Cohesion: 0.5
Nodes (1): IEventBus

### Community 39 - "IClientRefRepository"
Cohesion: 0.5
Nodes (1): IClientRefRepository

### Community 40 - "ISavingsAccountRepository"
Cohesion: 0.5
Nodes (1): ISavingsAccountRepository

### Community 41 - "IProductRefRepository"
Cohesion: 0.5
Nodes (1): IProductRefRepository

### Community 42 - "ArchitectureTests"
Cohesion: 0.5
Nodes (1): ArchitectureTests

### Community 43 - "DependencyInjection"
Cohesion: 0.5
Nodes (1): DependencyInjection

### Community 44 - "SavingsAccountUnitOfWork"
Cohesion: 0.5
Nodes (2): ISavingsAccountUnitOfWork, SavingsAccountUnitOfWork

### Community 45 - "SavingsAccountReadRepository"
Cohesion: 0.5
Nodes (2): ISavingsAccountReadRepository, SavingsAccountReadRepository

### Community 46 - "InitialSavings"
Cohesion: 0.5
Nodes (2): CoreBanking.Accounts.Infrastructure.Persistence.Migrations, InitialSavings

### Community 47 - "ArchitectureTests"
Cohesion: 0.5
Nodes (1): ArchitectureTests

### Community 48 - "DependencyInjection"
Cohesion: 0.5
Nodes (1): DependencyInjection

### Community 49 - "ClientReadRepository"
Cohesion: 0.5
Nodes (2): IClientReadRepository, ClientReadRepository

### Community 50 - "UnitOfWork"
Cohesion: 0.5
Nodes (2): IUnitOfWork, UnitOfWork

### Community 51 - "InitialClients"
Cohesion: 0.5
Nodes (2): CoreBanking.Clients.Infrastructure.Persistence.Migrations, InitialClients

### Community 52 - "IClientRepository"
Cohesion: 0.5
Nodes (1): IClientRepository

### Community 53 - "ArchitectureTests"
Cohesion: 0.5
Nodes (1): ArchitectureTests

### Community 54 - "ISavingsProductRepository"
Cohesion: 0.5
Nodes (1): ISavingsProductRepository

### Community 55 - "ISavingsProductReadRepository"
Cohesion: 0.5
Nodes (1): ISavingsProductReadRepository

### Community 56 - "DependencyInjection"
Cohesion: 0.5
Nodes (1): DependencyInjection

### Community 57 - "ProductUnitOfWork"
Cohesion: 0.5
Nodes (2): IProductUnitOfWork, ProductUnitOfWork

### Community 58 - "InitialProducts"
Cohesion: 0.5
Nodes (2): CoreBanking.Products.Infrastructure.Persistence.Migrations, InitialProducts

### Community 59 - "IOutboxDbContext"
Cohesion: 0.67
Nodes (1): IOutboxDbContext

### Community 60 - "ISavingsAccountReadRepository"
Cohesion: 0.67
Nodes (1): ISavingsAccountReadRepository

### Community 61 - "ISavingsAccountUnitOfWork"
Cohesion: 0.67
Nodes (1): ISavingsAccountUnitOfWork

### Community 62 - "IClientReadRepository"
Cohesion: 0.67
Nodes (1): IClientReadRepository

### Community 63 - "IUnitOfWork"
Cohesion: 0.67
Nodes (1): IUnitOfWork

### Community 64 - "IProductUnitOfWork"
Cohesion: 0.67
Nodes (1): IProductUnitOfWork

### Community 65 - "OutboxProcessorOptions"
Cohesion: 1.0
Nodes (1): OutboxProcessorOptions

### Community 66 - "IAuditable"
Cohesion: 1.0
Nodes (1): IAuditable

### Community 67 - "IDomainEvent"
Cohesion: 1.0
Nodes (1): IDomainEvent

### Community 68 - "IDateTimeProvider"
Cohesion: 1.0
Nodes (1): IDateTimeProvider

### Community 69 - "ICurrentUser"
Cohesion: 1.0
Nodes (1): ICurrentUser

### Community 70 - "OutboxMessage"
Cohesion: 1.0
Nodes (1): OutboxMessage

### Community 71 - "InboxMessage"
Cohesion: 1.0
Nodes (1): InboxMessage

### Community 72 - "KafkaOptions"
Cohesion: 1.0
Nodes (1): KafkaOptions

### Community 73 - "ClientRef"
Cohesion: 1.0
Nodes (1): ClientRef

### Community 74 - "ProductRef"
Cohesion: 1.0
Nodes (1): ProductRef

### Community 76 - "Program"
Cohesion: 1.0
Nodes (1): Program

### Community 77 - "EF Core 10"
Cohesion: 1.0
Nodes (2): EF Core 10, Optimistic Concurrency (Version token)

## Knowledge Gaps
- **40 isolated node(s):** `JsonSerializerOptions`, `OutboxProcessorOptions`, `OutboxProcessorOptions`, `KafkaContainer`, `List` (+35 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **Thin community `SavingsProduct Unit Tests`** (10 nodes): `SavingsProductTests`, `.Create_raises_SavingsProductCreated_with_correct_rate()`, `.Create_with_empty_name_throws_DomainException()`, `.Create_with_empty_shortname_throws_DomainException()`, `.Create_with_valid_args_returns_active_product_and_raises_event()`, `.Currency_Of_normalizes_code_to_uppercase()`, `.Currency_Of_validates_code_length()`, `.Currency_Of_with_negative_decimal_places_throws_DomainException()`, `.DefaultInterestSettings()`, `SavingsProductTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Clients Consumer Tests`** (9 nodes): `ClientsConsumerTests`, `.ClientActivated_sets_IsActive_true_on_existing_ref()`, `.ClientActivated_when_ref_not_found_and_duplicate_is_skipped_entirely()`, `.ClientActivated_when_ref_not_found_marks_inbox_and_returns_without_upserting()`, `.ClientRegistered_upserts_ClientRef_with_IsActive_false()`, `.Duplicate_ClientRegistered_is_skipped()`, `.Serialize()`, `.Unknown_event_type_is_ignored_gracefully()`, `ClientsConsumerTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Products Consumer Tests`** (7 nodes): `ProductsConsumerTests`, `.Duplicate_event_is_skipped_via_inbox()`, `.SavingsProductCreated_event_upserts_ProductRef()`, `.SavingsProductCreated_marks_inbox_and_saves()`, `.Serialize()`, `.Unknown_event_type_is_ignored_gracefully()`, `ProductsConsumerTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ClientTests`** (7 nodes): `ClientTests`, `.Activate_from_pending_sets_active_and_date()`, `.Activate_raises_ClientActivated_event()`, `.Activate_when_already_active_throws_DomainException()`, `.Register_creates_pending_client_and_raises_event()`, `.Register_with_empty_name_throws_DomainException()`, `ClientTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `CreateSavingsProductHandlerTests`** (7 nodes): `CreateSavingsProductHandlerTests`, `.CreateSavingsProduct_handler_persists_and_returns_id()`, `.GetById_returns_dto_when_found()`, `.GetById_throws_NotFoundException_when_not_found()`, `.ListSavingsProducts_returns_all()`, `.ValidCommand()`, `CreateSavingsProductHandlerTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IntegrationEventContractTests`** (6 nodes): `IntegrationEventContractTests`, `.ClientActivated_serializes_required_consumer_fields()`, `.ClientRegistered_serializes_required_consumer_fields()`, `.SavingsProductCreated_serializes_required_consumer_fields()`, `JsonSerializerOptions`, `IntegrationEventContractTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `RegisterClientHandlerTests`** (6 nodes): `RegisterClientHandlerTests`, `.ActivateClient_loads_client_activates_and_saves()`, `.ActivateClient_throws_NotFoundException_when_not_found()`, `.GetClientById_returns_dto_or_throws_NotFoundException()`, `.Handle_persists_client_and_returns_id()`, `RegisterClientHandlerTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ExceptionTests`** (5 nodes): `ExceptionTests`, `.DomainException_exposes_code_and_message()`, `.NotFoundException_formats_message()`, `.ValidationException_exposes_errors_dictionary()`, `ExceptionTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `InboxService`** (5 nodes): `IInboxService`, `InboxService`, `.HasProcessedAsync()`, `.MarkProcessedAsync()`, `InboxService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ProductRefRepository`** (5 nodes): `IProductRefRepository`, `ProductRefRepository`, `.FindAsync()`, `.UpsertAsync()`, `ProductRefRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `SavingsAccountRepository`** (5 nodes): `ISavingsAccountRepository`, `SavingsAccountRepository`, `.Add()`, `.FindAsync()`, `SavingsAccountRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ClientRefRepository`** (5 nodes): `IClientRefRepository`, `ClientRefRepository`, `.FindAsync()`, `.UpsertAsync()`, `ClientRefRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `SystemCurrentUser`** (5 nodes): `ICurrentUser`, `SystemCurrentUser.cs`, `SystemCurrentUser.cs`, `SystemCurrentUser.cs`, `SystemCurrentUser`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `UtcDateTimeProvider`** (5 nodes): `IDateTimeProvider`, `UtcDateTimeProvider.cs`, `UtcDateTimeProvider.cs`, `UtcDateTimeProvider.cs`, `UtcDateTimeProvider`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ClientRepository`** (5 nodes): `IClientRepository`, `ClientRepository`, `.Add()`, `.FindAsync()`, `ClientRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `SavingsProductReadRepository`** (5 nodes): `ISavingsProductReadRepository`, `SavingsProductReadRepository`, `.FindDtoAsync()`, `.ListAsync()`, `SavingsProductReadRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `SavingsProductRepository`** (5 nodes): `ISavingsProductRepository`, `SavingsProductRepository`, `.Add()`, `.FindAsync()`, `SavingsProductRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IInboxService`** (4 nodes): `IInboxService`, `.HasProcessedAsync()`, `.MarkProcessedAsync()`, `IInboxService.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ExceptionToProblemDetailsHandler`** (4 nodes): `ExceptionToProblemDetailsHandler`, `.TryHandleAsync()`, `IExceptionHandler`, `ExceptionHandlerMiddleware.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Entity`** (4 nodes): `Entity`, `.Equals()`, `.GetHashCode()`, `Entity.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IEventBus`** (4 nodes): `IEventBus`, `.PublishAsync()`, `.PublishRawAsync()`, `IEventBus.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IClientRefRepository`** (4 nodes): `IClientRefRepository`, `.FindAsync()`, `.UpsertAsync()`, `IClientRefRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ISavingsAccountRepository`** (4 nodes): `ISavingsAccountRepository`, `.Add()`, `.FindAsync()`, `ISavingsAccountRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IProductRefRepository`** (4 nodes): `IProductRefRepository`, `.FindAsync()`, `.UpsertAsync()`, `IProductRefRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ArchitectureTests`** (4 nodes): `ArchitectureTests`, `.Application_should_not_depend_on_Infrastructure()`, `.Domain_should_not_depend_on_Application()`, `ArchitectureTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `DependencyInjection`** (4 nodes): `DependencyInjection`, `.AddSavingsAccountsInfrastructure()`, `.DomainEventToIntegrationEventMap()`, `DependencyInjection.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `SavingsAccountUnitOfWork`** (4 nodes): `ISavingsAccountUnitOfWork`, `SavingsAccountUnitOfWork`, `.SaveChangesAsync()`, `SavingsAccountUnitOfWork.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `SavingsAccountReadRepository`** (4 nodes): `ISavingsAccountReadRepository`, `SavingsAccountReadRepository`, `.FindDtoAsync()`, `SavingsAccountReadRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `InitialSavings`** (4 nodes): `CoreBanking.Accounts.Infrastructure.Persistence.Migrations`, `InitialSavings`, `.BuildTargetModel()`, `20260606184750_InitialSavings.Designer.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ArchitectureTests`** (4 nodes): `ArchitectureTests`, `.Application_should_not_depend_on_Infrastructure()`, `.Domain_should_not_depend_on_Application()`, `ArchitectureTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `DependencyInjection`** (4 nodes): `DependencyInjection`, `.AddClientsInfrastructure()`, `.DomainEventToIntegrationEventMap()`, `DependencyInjection.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ClientReadRepository`** (4 nodes): `IClientReadRepository`, `ClientReadRepository`, `.FindDtoAsync()`, `ClientReadRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `UnitOfWork`** (4 nodes): `IUnitOfWork`, `UnitOfWork`, `.SaveChangesAsync()`, `UnitOfWork.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `InitialClients`** (4 nodes): `CoreBanking.Clients.Infrastructure.Persistence.Migrations`, `InitialClients`, `.BuildTargetModel()`, `20260606181446_InitialClients.Designer.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IClientRepository`** (4 nodes): `IClientRepository`, `.Add()`, `.FindAsync()`, `IClientRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ArchitectureTests`** (4 nodes): `ArchitectureTests`, `.Application_should_not_depend_on_Infrastructure()`, `.Domain_should_not_depend_on_Application()`, `ArchitectureTests.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ISavingsProductRepository`** (4 nodes): `ISavingsProductRepository`, `.Add()`, `.FindAsync()`, `ISavingsProductRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ISavingsProductReadRepository`** (4 nodes): `ISavingsProductReadRepository`, `.FindDtoAsync()`, `.ListAsync()`, `ISavingsProductReadRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `DependencyInjection`** (4 nodes): `DependencyInjection`, `.AddProductsInfrastructure()`, `.DomainEventToIntegrationEventMap()`, `DependencyInjection.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ProductUnitOfWork`** (4 nodes): `IProductUnitOfWork`, `ProductUnitOfWork`, `.SaveChangesAsync()`, `ProductUnitOfWork.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `InitialProducts`** (4 nodes): `CoreBanking.Products.Infrastructure.Persistence.Migrations`, `InitialProducts`, `.BuildTargetModel()`, `20260606183413_InitialProducts.Designer.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IOutboxDbContext`** (3 nodes): `IOutboxDbContext`, `.SaveChangesAsync()`, `IOutboxDbContext.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ISavingsAccountReadRepository`** (3 nodes): `ISavingsAccountReadRepository`, `.FindDtoAsync()`, `ISavingsAccountReadRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ISavingsAccountUnitOfWork`** (3 nodes): `ISavingsAccountUnitOfWork`, `.SaveChangesAsync()`, `ISavingsAccountUnitOfWork.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IClientReadRepository`** (3 nodes): `IClientReadRepository`, `.FindDtoAsync()`, `IClientReadRepository.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IUnitOfWork`** (3 nodes): `IUnitOfWork`, `.SaveChangesAsync()`, `IUnitOfWork.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IProductUnitOfWork`** (3 nodes): `IProductUnitOfWork`, `.SaveChangesAsync()`, `IProductUnitOfWork.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `OutboxProcessorOptions`** (2 nodes): `OutboxProcessorOptions`, `OutboxProcessorOptions.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IAuditable`** (2 nodes): `IAuditable`, `IAuditable.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IDomainEvent`** (2 nodes): `IDomainEvent`, `IDomainEvent.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `IDateTimeProvider`** (2 nodes): `IDateTimeProvider`, `IDateTimeProvider.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ICurrentUser`** (2 nodes): `ICurrentUser`, `ICurrentUser.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `OutboxMessage`** (2 nodes): `OutboxMessage`, `OutboxMessage.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `InboxMessage`** (2 nodes): `InboxMessage`, `InboxMessage.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `KafkaOptions`** (2 nodes): `KafkaOptions`, `KafkaOptions.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ClientRef`** (2 nodes): `ClientRef`, `ClientRef.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `ProductRef`** (2 nodes): `ProductRef`, `ProductRef.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `Program`** (2 nodes): `Program`, `Program.cs`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.
- **Thin community `EF Core 10`** (2 nodes): `EF Core 10`, `Optimistic Concurrency (Version token)`
  Too small to be a meaningful cluster - may be noise or needs more connections extracted.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What connects `JsonSerializerOptions`, `OutboxProcessorOptions`, `OutboxProcessorOptions` to the rest of the system?**
  _40 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Architecture Plan & Deployment` be split into smaller, more focused modules?**
  _Cohesion score 0.06 - nodes in this community are weakly interconnected._
- **Should `Account Lifecycle Commands (CQRS)` be split into smaller, more focused modules?**
  _Cohesion score 0.06 - nodes in this community are weakly interconnected._
- **Should `SavingsAccount Aggregate` be split into smaller, more focused modules?**
  _Cohesion score 0.1 - nodes in this community are weakly interconnected._
- **Should `Read/Write DbContexts` be split into smaller, more focused modules?**
  _Cohesion score 0.09 - nodes in this community are weakly interconnected._
- **Should `Kafka Consumers & Gateway Middleware` be split into smaller, more focused modules?**
  _Cohesion score 0.11 - nodes in this community are weakly interconnected._
- **Should `REST API Controllers` be split into smaller, more focused modules?**
  _Cohesion score 0.11 - nodes in this community are weakly interconnected._