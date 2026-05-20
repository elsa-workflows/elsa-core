# Elsa Workflows Core — Architecture pattern assessment

| Field | Value |
|---|---|
| **Repository / project** | elsa-workflows/elsa-core |
| **Git ref** | release/3.7.0 |
| **Version** | 3.7.0 |
| **Assessment date (UTC)** | 2026-05-20T00:00:00Z |
| **Assessment method** | Static code analysis, Documentation review, Architecture pattern mapping |
| **Model and tools** | Claude Sonnet 4.6 · Static code analysis · Architecture Pattern Catalog v0.3 |
| **Assessment scope** | All 50 source projects in src/ — core engine + all modules |
| **Related documents** | elsa-core-profile.md, elsa-core-software-quality.md, elsa-core-iso25010.md |

> **AI-assisted assessment — human review required**
> This document was produced by static analysis of the release/3.7.0 branch. No tests were executed and no runtime behaviour was observed. Ratings reflect what the framework provides out of the box; patterns marked Extensible require host-application code. Confirm every critical finding against the live codebase before using this assessment for architectural decisions.

---

## Coverage level legend

| Symbol | Level | Meaning |
|---|---|---|
| ✅ | Fully covered | First-class, production-ready support out of the box |
| 🟡 | Partially covered | Addresses the problem but with limitations or missing capabilities |
| 🔧 | Extensible / bring your own | Provides the hook; implementation is up to you |
| ❌ | Not covered | Outside scope; a separate tool is needed |

---

## 1. Integration patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| API gateway | ❌ | Elsa exposes REST endpoints (via `Elsa.Workflows.Api`) and provides auth middleware but is not an API gateway product. Routing, rate limiting, and cross-cutting enforcement belong to the host (YARP, NGINX, AWS API Gateway). |
| Request-reply (REST/RPC/gRPC) | ✅ | The `Elsa.Workflows.Api` module delivers a full REST API (FastEndpoints). `SendHttpRequest` and `SendHttpRequestBase` activities enable synchronous HTTP calls from within workflows. OpenAPI/Swagger docs are generated in development mode. |
| Asynchronous request-reply | ✅ | The bookmark/stimulus subsystem is a first-class async request-reply mechanism: a workflow suspends on a `Bookmark` and resumes when a matching stimulus arrives (`IStimulusSender`, `IWorkflowInbox`). `BackgroundWorkflowDispatcher` decouples dispatch from execution. |
| Message channel / message bus | 🟡 | The internal `Elsa.Mediator` module provides in-process channels (`ICommandsChannel`, `INotificationsChannel`, `IJobsChannel`) with background, sequential, and parallel publishing strategies. There is no native integration with external brokers (RabbitMQ, Azure Service Bus, etc.); the docker-compose file ships a RabbitMQ service only as a development convenience; no code wire-up exists in the library. |
| Publish-subscribe | 🟡 | Internal pub-sub is first class: `INotificationSender` broadcasts `INotification` events to all registered `INotificationHandler<T>` instances. `IEventPublisher` / `PublishEvent` activity allows workflow-level event broadcasting that can trigger or resume other workflows. External broker pub-sub is not covered. |
| Webhook / callback | 🟡 | The `Elsa.Http` module provides `HttpEndpoint` activities that act as incoming webhook receivers. `SendHttpRequest` can call external URLs. However, there is no outbound webhook dispatch service (no `IWebhookDispatcher`, no HMAC signing, no retry queue for outbound callbacks); host applications must build this. |
| Point-to-point channel | 🟡 | The bookmark queue (`IBookmarkQueue`, `BookmarkQueueWorker`) implements exactly-one-receiver semantics for work items. The distributed variant (`DistributedBookmarkQueueWorker`) adds distributed locking to prevent duplicate processing. No external broker-backed point-to-point channel. |
| Data pipeline / ETL | 🟡 | `BulkDispatchWorkflows` and `ForEach`/`ParallelForEach` activities can orchestrate batch processing pipelines over data collections. No native connector framework for external data sources or built-in ETL transforms. |
| File-based exchange | 🟡 | `Elsa.WorkflowProviders.BlobStorage` (FluentStorage) allows workflow definitions to be loaded from blob/file storage. `DownloadHttpFile` / `WriteFileHttpResponse` activities handle file transfer in HTTP workflows. No generic file-event trigger or file watcher built in. |
| Anti-corruption layer (ACL) | 🔧 | The `IActivityStateFilter` / `IActivityStateFilterManager` pipeline allows host code to intercept and transform activity I/O. The reference implementation (`HttpRequestAuthenticationHeaderFilter`) masks sensitive headers. A full ACL translating a legacy data model must be written by the host application. |
| Strangler fig | 🔧 | No explicit strangler-fig tooling. The modular feature system (`IShellFeature`, `FeatureBase`) and the ability to register custom `IActivityProvider` implementations allow incremental replacement of legacy process logic, but the routing and traffic-splitting infrastructure is external. |
| Messaging bridge | ❌ | No broker bridge/adapter; connecting incompatible messaging systems requires an external component. |
| Gateway aggregation | 🔧 | Workflow activities can call multiple HTTP services and aggregate responses, but there is no dedicated API gateway aggregation layer. Custom composite activities can serve this role. |
| Gateway offloading | ❌ | Cross-cutting concerns (TLS termination, rate limiting, request correlation) must be handled by the host's API gateway, not Elsa. |
| Ambassador | ❌ | No sidecar/ambassador proxy pattern; network concerns are handled by ASP.NET Core middleware or external infrastructure. |
| Sidecar | ❌ | Not applicable to a workflow engine library. |
| Service mesh | ❌ | Not applicable; requires external infrastructure (Istio, Linkerd). |

---

## 2. Processing and workflow patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| Saga / process orchestration | ✅ | This is the central purpose of Elsa. Long-running, multi-step, stateful workflow orchestration is first-class. Durable state is checkpointed between activities, compensation is partially supported via `IIncidentStrategy`, and the `Alterations` subsystem allows runtime correction of running workflows. |
| Choreography | 🟡 | The internal `INotificationSender`/`IEventPublisher` + `Event` activity allow decentralised event-driven coordination between concurrently running workflows. However, there is no visual choreography view, and cross-workflow event schemas are not enforced. |
| Competing consumers | ✅ | `BookmarkQueueWorker` processes items from the `IBookmarkQueue`; multiple application instances compete via `DistributedBookmarkQueueWorker` (file-system distributed lock, Medallion.Threading). Named dispatcher channels (`DispatcherChannel`) allow workload segmentation. |
| Priority queue | ❌ | No priority queue support in the bookmark queue or dispatcher. All queued items are processed FIFO. |
| Sequential convoy | 🟡 | `SingletonStrategy` and `CorrelatedSingletonStrategy` activation validators prevent concurrent instances on the same correlation ID, effectively serialising processing per correlation key. No general partitioned sequential convoy for arbitrary message streams. |
| Pipes and filters | ✅ | First-class dual pipeline: `IWorkflowExecutionPipeline` and `IActivityExecutionPipeline` each have a composable middleware builder. Workflow and activity execution pass through registered middleware stages. |
| Batch processing | 🟡 | `BulkDispatchWorkflows` and `ForEach`/`ParallelForEach` support batch iteration. `RestartInterruptedWorkflowsTask` processes stale instances in configurable batch sizes. No dedicated batch job scheduler (Quartz, Hangfire) is included; the built-in scheduler is time-trigger based. |
| Stream processing | ❌ | No continuous stream processing. Workflows are discrete instances; there is no Kafka/EventHub consumer or reactive stream pipeline. |
| Rule engine | ❌ | No embedded rule engine. Conditional logic lives in `If`/`Switch` activities driven by C#/JavaScript/Liquid expressions. For an external rule engine, a custom activity is needed. |
| Calculation engine | 🔧 | Multi-language expression evaluation (C#, JavaScript, Python, Liquid, ElsaScript) can serve as a calculation engine within activities, but there is no dedicated domain-specific calculation runtime. |
| Document / output generation | ❌ | No PDF, Word, or structured document generation. Custom activities calling external rendering services are needed. |
| Scheduler agent supervisor | ✅ | `RestartInterruptedWorkflowsTask` (recurring task, distributed lock guarded) detects workflows that have exceeded the `InactivityThreshold` and restarts them. `HeartbeatGenerator` monitors per-instance liveness. This is a first-class supervisor pattern. |
| Claim check | 🔧 | The `IStorageDriver` abstraction (with `MemoryStorageDriver` and blob-backed drivers) allows large variable data to be stored externally and referenced by ID within the workflow state. The check-in/check-out mechanics must be coded in custom activities. |
| Compensating transaction | 🟡 | `IIncidentStrategy` hooks (e.g., `ContinueWithIncidentsStrategy`) handle faults at the activity level, but there is no out-of-the-box backward compensation chain (no "undo stack"). Compensation logic must be manually modelled as workflow branches or alteration handlers. |

---

## 3. Data management patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| CQRS | 🟡 | Internal mediator (`ICommandSender` / `INotificationSender`) separates commands from notifications. Workflow management and runtime stores are separate EF Core `DbContext` instances, enabling independent read/write tuning. However, there are no dedicated read-model projections; queries hit the same store as writes. |
| Event sourcing | ❌ | Workflow execution is snapshot-based (full state serialised to `WorkflowState`), not event-sourced. The `WorkflowExecutionLogStore` provides an append-only execution log for auditing but is not a replayable event stream from which state can be reconstructed. |
| Cache / cache-aside | ✅ | `Elsa.Caching` module with `IMemoryCache` is used throughout. `CachingTriggerStore` and `CachingWorkflowRuntimeFeature` implement cache-aside for triggers and workflow definitions. Cache invalidation is notification-driven (`InvalidateTriggersCache`, `InvalidateWorkflowsCache`). |
| Read replica | 🔧 | EF Core supports read replicas via connection string configuration; Elsa's `WorkflowManagementPersistenceFeature` and `WorkflowRuntimePersistenceFeature` can be pointed at read replicas. No first-class API; host must configure EF Core accordingly. |
| Data warehouse / data mart | ❌ | No analytics or data warehouse integration. |
| Materialized view | ❌ | No materialized view pattern. Aggregations are computed at query time. |
| Index table | 🟡 | EF Core migrations create indices on frequently queried columns (workflow instance status, correlation ID, definition ID). These are hardcoded in migration files and not dynamically configurable via an index-table abstraction. |
| Sharding | ❌ | No sharding support. Multi-tenancy isolation uses separate databases per tenant via `Tenant.GetConnectionString()`, which approximates horizontal partitioning, but there is no query router or rebalancing tooling. |
| External configuration store | 🟡 | `Elsa.KeyValues` provides a key-value store (`IKeyValueStore`) that can persist arbitrary named values. `ConfigurationTenantsProvider` and `ConfigurationBasedUserProvider` load data from `IConfiguration` (which can be backed by Azure App Config, AWS SSM, etc.). No dedicated dynamic configuration store abstraction. |
| Binary / blob storage | ✅ | `Elsa.WorkflowProviders.BlobStorage` (FluentStorage) provides a pluggable `IBlobStorageProvider` for loading workflow definitions from blob storage. `IStorageDriver` allows workflow variable data to be externally stored. |
| Search index | ❌ | No full-text search index. Workflow instance and definition queries use EF Core `IQueryable` filtering only. |
| Database per service | 🟡 | Management and Runtime each have their own EF Core `DbContext` (`ManagementElsaDbContext`, `RuntimeElsaDbContext`) and can be pointed at separate databases. Tenant-specific connection strings are supported. However, all modules in a shell share the same database by default; isolation is opt-in. |
| Shared database | ✅ | Default deployment uses a single SQLite/PostgreSQL/SQL Server/MySQL/Oracle database shared across all modules. This is the expected configuration for single-server deployments. |

---

## 4. Reliability and resilience patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| Circuit breaker | 🔧 | `IResilienceStrategy` (Polly) is the extension point. `HttpResilienceStrategy` registers a retry policy; a circuit-breaker `IResilienceStrategy` can be added by calling `AddResilienceStrategyType<T>()` in `ResilienceFeature`. No circuit-breaker strategy is shipped out of the box. |
| Retry with exponential backoff | ✅ | `HttpResilienceStrategy` provides configurable retry with exponential backoff, jitter, and max attempts via Polly `RetryStrategyOptions`. The `ResilientActivityInvoker` applies the strategy and records `RetryAttemptRecord`s for diagnostics. Any `IResilientActivity` can use this. |
| Bulkhead | 🔧 | Named dispatcher channels (`DispatcherChannel`) logically partition workflow execution queues, providing bulkhead-like isolation between workload classes. No Polly `BulkheadOptions` strategy is shipped; one can be registered via `IResilienceStrategy`. |
| Queue-based load levelling | 🟡 | `BookmarkQueueWorker` decouples workflow execution from stimulus arrival, smoothing spikes. The queue is stored in a database table (not a durable message broker), so it lacks broker-level durability and dead-letter support. |
| Rate limiting / throttling | ❌ | No rate-limiting middleware or activity throttling. ASP.NET Core rate-limiting middleware can be added by the host application. |
| Idempotency | 🟡 | `SingletonStrategy` and `CorrelatedSingletonStrategy` prevent duplicate workflow instances per correlation key. Bookmark hashing (`IStimulusHasher`) deduplicates stimulus delivery. However, there is no general-purpose idempotency key store for idempotent API calls or external operations. |
| Health endpoint monitoring | 🟡 | `services.AddHealthChecks()` is wired in the reference server (`Elsa.Server.Web`) and `app.MapHealthChecks("/")` is configured. However, no Elsa-specific health checks (e.g., workflow runtime healthy, scheduler running, database connected) are registered in the framework; the host gets a bare ASP.NET Core health endpoint. |
| Leader election | 🟡 | `InstanceHeartbeatMonitorService` uses `IDistributedLockProvider` (Medallion.Threading / file-system lock) to elect a single monitor instance across a cluster. `SingleNodeTaskAttribute` marks tasks that should run on only one node; `TaskExecutor` honours this attribute. True Raft/etcd-based leader election is not included. |
| Deployment stamps | ❌ | No first-class multi-region stamp deployment tooling. |
| Geode | ❌ | No geo-distribution support. |
| Quarantine | ❌ | No quarantine/validation gate for inbound messages or data. |
| Timeout | ✅ | `Delay`, `StartAt`, `Timer`, and `Cron` activities implement time-based waits. `CancellationToken` propagation allows workflow-level and activity-level timeout cancellation. `JintOptions` exposes a JavaScript execution timeout. |

---

## 5. Security patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| Authentication (AuthN) | ✅ | `Elsa.Identity` ships JWT Bearer and API key authentication out of the box (`DefaultAuthenticationFeature`). JwtBearer and `AspNetCore.Authentication.ApiKey` are configured via a multi-scheme policy scheme. User/application/role providers are pluggable (configuration-based, store-based, or custom). |
| Authorisation (AuthZ) RBAC/ABAC | 🟡 | Role-based authorisation is implemented: `Role` entities carry a `Permissions` collection (string claims), and `IAuthorizationPolicy` is applied per endpoint. There is no ABAC (attribute-based) engine; fine-grained resource-level policies beyond role+permission strings are not supported. |
| Token service / OAuth 2.0 / OIDC | 🟡 | `IAccessTokenIssuer` issues JWT access and refresh tokens (RS256/HS256). The identity module does not act as a full OAuth 2.0 authorization server (no authorization code flow, no PKCE, no discovery endpoint). For production SSO, an external IdP (Keycloak, Azure AD) should be used; Elsa's token service is intended for machine-to-machine and Studio authentication. |
| Federated identity | 🔧 | `DefaultAuthenticationFeature` registers a JWT Bearer handler that accepts tokens from any configured issuer. Plugging in an external OIDC IdP requires host-level configuration of `JwtBearerOptions` (audience, authority). No built-in OIDC callback flow. |
| Valet key | ✅ | `Elsa.SasTokens` provides `ITokenService` (ASP.NET Core Data Protection backed) for creating time-limited, signed access tokens. The `HttpEndpoint` activity uses SAS tokens to generate caller-specific signed URLs. |
| Encryption at rest and in transit | 🔧 | HTTPS is handled by the ASP.NET Core/Kestrel host. Data-at-rest encryption requires database-level or column-level encryption configured outside Elsa. ASP.NET Core Data Protection backs `SasTokens`. No field-level encryption of workflow variables is provided. |
| Payload signing | 🟡 | SAS token service signs payloads using Data Protection. No general HMAC webhook signature for outbound HTTP calls; the `SendHttpRequest` activity supports a configurable `Authorization` header but not automatic payload signing. |
| Secrets management | ❌ | No secrets manager integration (HashiCorp Vault, Azure Key Vault, AWS Secrets Manager). Sensitive configuration is read from `IConfiguration` which must be backed by a secrets provider at the host level. |
| Defence in depth | 🟡 | Multiple independent layers are present: JWT + API key auth, ASP.NET Core authorization policies, `IActivityStateFilter` data masking, sandboxed expression evaluation (Jint JS engine). However, some layers require explicit host configuration; they are not automatically composed. |
| Zero trust / least privilege | 🔧 | Permission strings on roles allow least-privilege API access. Multi-tenancy isolation is available. `LocalHostPermissionRequirement` restricts the security-root policy to localhost by default. A full zero-trust implementation (per-resource, per-request verification, mutual TLS) is not built in. |
| Audit log | ✅ | `IWorkflowExecutionLogStore` persists an immutable, per-activity execution log record for every workflow run, including activity ID, instance ID, timestamps, and custom event data. The log is queryable via the REST API. This constitutes a functional workflow audit trail. |
| Data masking / tokenisation | 🟡 | `IActivityStateFilter` / `IActivityStateFilterManager` provides a framework for masking activity I/O before persisting state. The reference filter (`HttpRequestAuthenticationHeaderFilter`) masks HTTP `Authorization` headers. General PII tokenisation must be implemented as custom filters. |

---

## 6. Communication and messaging patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| Notification / push | 🟡 | SignalR hub (`WorkflowInstanceHub`, `RealTimeWorkflowUpdatesFeature`) pushes workflow progress events to connected Studio clients. The feature is available but disabled by default in the reference server (awaiting authenticated Studio requests). Server-sent events and WebSockets for other consumers are not built in. |
| Mail service (transactional email) | ❌ | No email activity or SMTP integration in the open-source library. The docker-compose file includes smtp4dev for local testing but there is no `SendEmail` activity or `IEmailSender` abstraction. |
| Scheduled job / cron | ✅ | `Cron`, `Timer`, `Delay`, and `StartAt` activities are first-class. `LocalScheduler` (in-memory, in-process) and `DefaultWorkflowScheduler` manage trigger schedules. `RecurringTask` / `SingleNodeTaskAttribute` pattern handles periodic maintenance tasks. |
| Dead letter channel | ❌ | No dead-letter queue. Unprocessable bookmark queue items are purged after TTL expiry (`BookmarkQueuePurgeOptions`); they are not routed to a dead-letter store for inspection or replay. |
| Message expiry / TTL | ✅ | `WorkflowInboxMessage` has an `ExpiresAt` field. `BookmarkQueuePurgeOptions.Ttl` controls item expiry. Stale items are purged by `PurgeBookmarkQueueRecurringTask`. |
| Correlation identifier | ✅ | Correlation ID is a first-class concept throughout: workflow instances carry `CorrelationId`, stimuli carry `StimulusMetadata.CorrelationId`, and `CorrelationStrategy` / `CorrelatedSingletonStrategy` use it for activation control and deduplication. |

---

## 7. Scalability and deployment patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| Horizontal scaling | 🟡 | `Elsa.Workflows.Runtime.Distributed` enables multi-node operation: `DistributedWorkflowRuntime`, `DistributedWorkflowClient` (with distributed locking via Medallion.Threading), and `DistributedBookmarkQueueWorker` coordinate across instances. The default lock provider is file-system based (suitable for single-machine multi-instance, not cross-host); production clustering requires a Redis/SQL distributed lock provider to be substituted. |
| Vertical scaling | ❌ | Not a pattern Elsa addresses; handled by infrastructure. |
| Auto-scaling | ❌ | No auto-scaling hooks. Kubernetes HPA or cloud provider auto-scaling can be applied externally. |
| Blue-green deployment | 🔧 | Workflow versioning (major/minor version on definitions) supports running old and new versions concurrently. Blue-green traffic routing is an infrastructure concern external to Elsa. |
| Canary deployment | 🔧 | Same as blue-green: version-based routing is possible but requires external traffic management. |
| Feature flags | ❌ | No feature flag mechanism; `IShellFeature` / `FeatureBase` are compile-time/startup-time module toggles, not runtime feature flags (no LaunchDarkly integration, no `IFeatureManager` from Microsoft.FeatureManagement). |
| Compute resource consolidation | ✅ | The library is designed to be embedded in a single .NET process alongside the host application. `Elsa.ModularServer.Web` and `Elsa.Server.Web` demonstrate collocating engine, API, scheduling, and management in one process. |
| Static content hosting | ❌ | Not applicable to the workflow engine library (static assets belong to Elsa Studio). |
| Serverless / FaaS | ❌ | No FaaS adapter. Elsa requires a long-running ASP.NET Core host. |

---

## 8. Observability and operations patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| Metrics, logs, traces (three pillars) | 🟡 | Structured `ILogger<T>` logging is used throughout. Polly telemetry listeners (`RetryTelemetryListener`) emit retry events. `WorkflowExecutionLogRecord` is a domain-level execution log. However, there is no first-party OpenTelemetry integration, no metrics export (Prometheus, OTLP), and no distributed trace propagation (W3C Trace Context headers are not automatically forwarded across workflow activity calls). |
| Distributed tracing | ❌ | No `ActivitySource` / `OpenTelemetry.Api` instrumentation in the library. Trace context must be propagated manually by host-application code. |
| Log aggregation | 🔧 | Standard `ILogger` output can be directed to any sink (Serilog, NLog, Application Insights) by the host. Elsa does not bundle a log aggregation stack. |
| Health check / readiness / liveness | 🟡 | ASP.NET Core `AddHealthChecks()` is configured in the reference server. No Elsa-specific health check providers (workflow runtime healthy, EF Core migration complete, scheduler running) are shipped in the library. |
| Chaos engineering | ❌ | No chaos engineering tooling. |
| Synthetic monitoring | ❌ | No synthetic monitoring built in. |
| Alert routing and on-call | ❌ | No alert routing; external observability platforms (PagerDuty, Datadog) must be used. |

---

## 9. Frontend and API design patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| Backend for frontend (BFF) | ❌ | No BFF pattern. The REST API (`Elsa.Workflows.Api`) is a general-purpose API shared by all consumers (Studio, integrators, scripts). |
| Multi-tenancy | ✅ | First-class multi-tenancy: `Elsa.Tenants` provides a pluggable tenant resolution pipeline (`ITenantResolverPipelineBuilder`), `ITenantStore`, per-tenant connection strings, and `TenantResolutionMiddleware`. Tenant isolation for workflow definitions and instances is supported via `TenantId` on all entities. |
| Server-side rendering (SSR) | ❌ | elsa-core is a server-side workflow library/engine; UI/UX patterns apply to Elsa Studio (separate repository), not to this library. |
| Progressive web app (PWA) | ❌ | elsa-core is a server-side workflow library/engine; UI/UX patterns apply to Elsa Studio (separate repository), not to this library. |
| Micro-frontends | ❌ | elsa-core is a server-side workflow library/engine; UI/UX patterns apply to Elsa Studio (separate repository), not to this library. |
| GraphQL API | ❌ | No GraphQL endpoint. The REST API uses FastEndpoints. |
| Hypermedia / HATEOAS | ❌ | No HATEOAS; REST responses return flat resource representations. |

---

## 10. Distributed systems coordination patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| Distributed locking | ✅ | `Medallion.Threading` (`IDistributedLockProvider`) is used in `DistributedWorkflowClient`, `DistributedBookmarkQueueWorker`, and `InstanceHeartbeatMonitorService`. The default provider is `FileSystemDistributedSynchronizationProvider` (single-machine). Redis, SQL Server, or PostgreSQL providers can be substituted via DI. |
| Two-phase commit (2PC) | ❌ | No distributed 2PC. Consistency is achieved through pessimistic distributed locking and idempotent retry, not atomic distributed transactions. |
| Consensus (Raft/Paxos) | ❌ | No consensus protocol. Leader/coordinator selection relies on optimistic distributed locking. |
| Eventual consistency / BASE | ✅ | The system is designed around eventual consistency: workflow dispatching is asynchronous, bookmark delivery may be delayed, state commits are periodic (configurable via `IWorkflowCommitStateHandler`). Callers must tolerate stale reads of workflow instance status. |
| Outbox pattern | ❌ | No transactional outbox. Notification publishing and workflow dispatching are not atomically bundled with database writes. In high-reliability scenarios, an external outbox (MassTransit Outbox, Wolverine) must be layered on. |
| Change data capture (CDC) | ❌ | No CDC integration. |
| Inbox pattern | 🟡 | `IWorkflowInbox` / `WorkflowInboxMessage` (now superseded by the Stimulus API) provides an inbox table that stores messages until they can be delivered to a matching workflow. The deduplication is hash-based (`IStimulusHasher`). The implementation is partial: it is marked `[Obsolete]` and has no dead-letter or replay mechanism. |

---

## 11. AI and intelligent system patterns

| Pattern | Coverage | Assessment |
|---|---|---|
| Model serving / inference endpoint | ❌ | No ML model serving; a custom HTTP activity calling an inference endpoint is required. |
| RAG | ❌ | No retrieval-augmented generation support. |
| AI agent orchestration | 🔧 | Elsa's long-running workflow orchestration primitives (human-in-loop via `HttpEndpoint` callback, event-driven resumption, Fork/Join, distributed execution) can serve as the control plane for multi-agent pipelines. This requires building agent activities; nothing is provided out of the box. |
| Human-in-the-loop | 🟡 | The bookmark/stimulus pattern natively supports suspending a workflow until a human action is received (e.g., `HttpEndpoint` callback, custom signal). The `Alterations` subsystem allows operators to manually modify running workflow state. There is no built-in task-assignment UI or human task service; custom activity + external task management is needed. |
| Shadow mode / champion-challenger | ❌ | No shadow execution mode. |

---

## 12–17. UX, accessibility, CX, information architecture, design system, frontend performance patterns

All patterns in categories 12 through 17 are rated **❌ Not covered**.

elsa-core is a server-side workflow library/engine. UI/UX patterns (UX design, accessibility, customer experience, information architecture, design systems, and frontend performance) apply to Elsa Studio, which is a separate repository (`elsa-workflows/elsa-studio`). These categories are not assessable for this library.

---

## Summary coverage matrix

| Category | ✅ Fully | 🟡 Partially | 🔧 Extensible | ❌ Not covered |
|---|---|---|---|---|
| 1. Integration patterns | 2 | 5 | 3 | 7 |
| 2. Processing and workflow patterns | 5 | 4 | 2 | 3 |
| 3. Data management patterns | 3 | 4 | 2 | 4 |
| 4. Reliability and resilience patterns | 2 | 4 | 3 | 3 |
| 5. Security patterns | 3 | 4 | 3 | 2 |
| 6. Communication and messaging patterns | 3 | 1 | 0 | 2 |
| 7. Scalability and deployment patterns | 1 | 1 | 3 | 4 |
| 8. Observability and operations patterns | 0 | 2 | 1 | 4 |
| 9. Frontend and API design patterns | 1 | 0 | 0 | 6 |
| 10. Distributed systems coordination | 2 | 1 | 0 | 4 |
| 11. AI and intelligent system patterns | 0 | 1 | 1 | 3 |
| 12–17. UX / frontend patterns (all) | 0 | 0 | 0 | all |
| **Totals (cat. 1–11)** | **22** | **27** | **18** | **42** |

---

## Architecture fit summary

### Right tool when:

- You need durable, long-running orchestration of multi-step business processes in a .NET application, including suspend/resume across process restarts.
- Your domain requires complex conditional branching, parallel execution (`Fork`, `Parallel`, `ParallelForEach`), and sequential coordination inside a single business process.
- You need human-in-the-loop workflows that pause on an HTTP callback or an external signal and resume after a person acts.
- You run on ASP.NET Core and want a REST API, a visual designer, and embedded scheduling in a single deployable unit.
- You need multi-tenant SaaS workflow automation with per-tenant connection strings and tenant-scoped workflow isolation.
- You want to invoke external HTTP services from within workflows and need retry-with-backoff (via Polly) built into those activity calls.
- Your workload is event-driven — triggering workflows from HTTP endpoints, cron schedules, or domain events — and you want durable correlation of long-lived process instances.
- You need runtime workflow correction (adding/modifying/cancelling running instances) via the Alterations API without redeployment.

### Wrong tool when:

- You need real-time stream processing of continuous data (Kafka, EventHub consumer pipelines) — Elsa is discrete-instance based.
- You need a full external message broker integration (RabbitMQ, Azure Service Bus publish/subscribe at scale) without writing a custom adapter.
- You need OpenTelemetry distributed tracing instrumentation out of the box — no `ActivitySource` instrumentation exists in this library.
- You need a feature-complete OAuth 2.0 authorization server (PKCE, authorization code flow, discovery endpoint) — the identity module provides tokens for Studio/API access only.
- You are targeting FaaS/serverless execution environments — Elsa requires a continuously running ASP.NET Core host.
- You need event sourcing with full replay capability — workflow state is snapshot-based, not an event-sourced log.
- You need a transactional outbox guarantee for publishing events atomically with database writes — this is not implemented.

### Elsa Workflows Core in context: named gaps with recommendations

| Gap | Severity | Concrete recommendation |
|---|---|---|
| No transactional outbox | High | In distributed deployments, dispatching a workflow command and committing the trigger state in separate steps can lead to duplicate or lost executions. Integrate MassTransit Outbox or Wolverine as the `IWorkflowDispatcher` backend to guarantee at-least-once delivery atomically with the database transaction. |
| Distributed lock provider defaults to file system | High | `FileSystemDistributedSynchronizationProvider` does not work across machines with separate file systems. Replace with `DistributedLock.Redis` or `DistributedLock.SqlServer` via a single DI registration before deploying multiple nodes. |
| No OpenTelemetry instrumentation | Medium | There are no `ActivitySource` spans, no metric counters, and no trace context propagation. Add an `IWorkflowExecutionMiddleware` and `IActivityExecutionMiddleware` that start and stop OpenTelemetry `Activity` spans; wire W3C `traceparent` headers through `SendHttpRequest` calls. |
| No circuit breaker shipped | Medium | `HttpResilienceStrategy` provides retry but no circuit breaker. A transient outage of a downstream HTTP service will exhaust retries on every workflow resumption. Implement a `CircuitBreakerResilienceStrategy : IResilienceStrategy` using `Polly.Extensions.Http` and register it in `ResilienceFeature`. |
| No dead-letter channel | Medium | Failed or undeliverable bookmark queue items are silently purged after TTL expiry with no visibility. Add a `DeadLetterBookmarkStore` that captures expired items with their last exception, plus a REST endpoint to inspect and replay them. |
| Health checks are host-owned, not framework-owned | Medium | The reference server wires `AddHealthChecks()` but registers no Elsa-specific checks. Ship `IHealthCheck` implementations for: workflow runtime responsive, EF Core migrations current, scheduler running, distributed lock provider reachable. This enables Kubernetes readiness/liveness probes to detect partial failures. |
| No secrets management integration | Medium | Sensitive data (API keys used in workflow activities, SMTP credentials) flows through `IConfiguration` and is persisted in workflow state unless filtered. Integrate ASP.NET Core's `ISecretManager` abstraction or add a `SecretsStorageDriver : IStorageDriver` backed by Azure Key Vault / HashiCorp Vault for variables tagged as sensitive. |
| Partial compensation / no undo stack | Medium | There is no built-in backward compensation chain. When a multi-step workflow fails mid-way, compensating already-completed steps must be hand-coded as workflow branches or `Alteration` handlers. Consider implementing a `CompensationScope` composite activity that records completed compensable steps and executes them in reverse order on fault. |
| Multi-tenancy isolation is shared-table by default | Low–Medium | By default, all tenants share EF Core tables with a `TenantId` column filter. A misconfigured query can expose cross-tenant data. For regulated environments, use per-tenant connection strings (already supported via `Tenant.GetConnectionString()`) and test isolation explicitly. |
| No feature flags at runtime | Low | `IShellFeature` toggles are startup-time only. If a rollout strategy (canary, A/B) for a new workflow version is required, integrate Microsoft.FeatureManagement or a similar SDK; Elsa's workflow versioning system alone is not sufficient for traffic-level rollout control. |