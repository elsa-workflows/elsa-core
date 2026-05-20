# Elsa Workflows — Technology Profile

| Field | Value |
|---|---|
| **Repository / project** | elsa-workflows/elsa-core |
| **Git ref** | release/3.7.0 |
| **Version** | 3.7.0 |
| **Assessment date (UTC)** | 2026-05-20T00:00:00Z |
| **Assessment method** | Static code analysis, Documentation review |
| **Model and tools** | Claude Sonnet 4.6 |
| **Assessment scope** | All 50 source projects in src/ — primary focus: Elsa.Workflows.Core, Elsa.Workflows.Runtime, Elsa.Workflows.Management, Elsa.Http, Elsa.Identity, Elsa.Tenants, Elsa.Resilience, Elsa.Scheduling, Elsa.Alterations |
| **Related documents** | elsa-core-architecture-patterns.md, elsa-core-software-quality.md, elsa-core-iso25010.md |

> ⚠️ **AI-assisted assessment — human review required**
>
> This document was produced by an AI model (Claude Sonnet 4.6) using static code
> analysis and structured pattern catalogs. AI-assisted assessments are based on pattern
> recognition and reasoning over source files and documentation. They may contain incorrect
> assumptions, missed context, or findings that do not apply to your specific operational
> environment.
>
> **This document is a draft input to a human review process — not a final approved report.**
> All findings should be verified by a qualified engineer or architect familiar with the
> codebase before being acted upon, communicated externally, or used as the basis for
> architectural or compliance decisions.

---

## 1. What Elsa Workflows Core Is

Elsa Workflows (elsa-core) is an open-source .NET library and engine for embedding workflow execution inside any .NET application. It provides the data model, execution runtime, persistence abstractions, expression evaluation, security, and optional HTTP/messaging integrations needed to author and run both short-running and long-running business processes entirely within a .NET host process.

Workflows can be authored in three ways: as typed C# classes derived from `WorkflowBase`, as JSON definitions loaded at runtime from a store or file system, or as ElsaScript `.elsa` text files parsed and compiled at startup. All three representations compile to the same in-memory activity graph that the execution engine evaluates.

**What Elsa Workflows Core is NOT:**

- It is not the Elsa Studio visual designer front-end. The Studio is a separate Blazor WebAssembly application that lives in a distinct repository (`elsa-studio`). This assessment covers only the engine and server library (`elsa-core`).
- It is not a cloud-hosted or managed workflow service. There is no SaaS offering — all components run in-process inside the consuming application.
- It is not a BPMN 2.0 engine and does not parse BPMN XML. The activity model is proprietary.
- It is not a low-code/no-code platform by itself. Designer capabilities require the separate Elsa Studio front-end.
- It is not a replacement for a message broker or event bus. While it can consume events via its bookmark/stimulus model, it relies on external infrastructure (RabbitMQ, Azure Service Bus via MassTransit) for durable message transport in distributed deployments.
- It is not a reporting or monitoring tool. Observability is delegated to OpenTelemetry exporters and external platforms.

**Scope statement:** This profile covers the `release/3.7.0` branch of `elsa-workflows/elsa-core`, specifically the 50 projects under `/src`. The primary focus modules are listed in the header.

---

## 2. Core Architecture

### 2.1 Target Frameworks and Deployment

Elsa targets `net8.0`, `net9.0`, and `net10.0`. The `Directory.Packages.props` file manages separate package version sets per framework, enabling multi-targeting from a single NuGet package publish. Binaries ship as Symbol Packages (`.snupkg`) with Source Link enabled. The server application (`Elsa.Server.Web`) is a standard ASP.NET Core Web project; no custom runtime host is required.

### 2.2 Module and Feature System

Every feature is a class that derives from `FeatureBase`. Features declare dependencies via `[DependsOn]` attributes, which the `IModule` implementation resolves into a topologically ordered apply sequence. Consumer code opts in through fluent extension methods on `IServiceCollection`:

```csharp
services.AddElsa(elsa =>
{
    elsa
        .UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(...).UseDistributedRuntime())
        .UseIdentity(identity => identity.UseConfigurationBasedUserProvider(...))
        .UseCSharp()
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
        .UseScheduling();
});
```

The top-level `AddElsa` method creates an `IModule` backed by `AppFeature` → `ElsaFeature`, which itself depends on `WorkflowsFeature`, `FlowchartFeature`, `DefaultWorkflowRuntimeFeature`, and `WorkflowManagementFeature`. All optional modules (identity, tenancy, resilience, expressions) are additive.

### 2.3 Execution Model

Elsa uses an async, scheduler-driven execution model. The entry point is `IWorkflowRunner`, which constructs a `WorkflowExecutionContext` and drives it through an `IWorkflowExecutionPipeline`. The pipeline is a middleware chain built with `IWorkflowExecutionPipelineBuilder`; the default middleware stack is:

| Middleware | Role |
|---|---|
| `EngineExceptionHandlingMiddleware` | Catches unhandled engine-level exceptions |
| `DefaultActivitySchedulerMiddleware` | Runs the scheduler loop, dequeuing and invoking `ActivityWorkItem` entries until the queue is empty |

The inner scheduler loop calls `IActivityInvoker`, which drives each activity through its own `IActivityExecutionPipeline`. The default activity pipeline includes:

| Middleware | Role |
|---|---|
| `ExceptionHandlingMiddleware` | Activity-level exception trapping and incident recording |
| `ExecutionLogMiddleware` | Writes `WorkflowExecutionLogEntry` records |
| `LoggingMiddleware` | Structured logger state injection |
| `NotificationPublishingMiddleware` | Mediator notifications before/after activity execution |
| `DefaultActivityInvokerMiddleware` | Resolves `IActivity.ExecuteAsync` and completes the context |

`WorkflowExecutionContext` holds the full mutable state of a running workflow instance: the activity scheduler, bookmark set, completion callbacks, output register, variable storage, correlation ID, tenant ID, and a reference to the `WorkflowGraph`. `ActivityExecutionContext` is the per-activity scope; it exposes `ExpressionExecutionContext` for expression evaluation and delegates state changes back to its parent `WorkflowExecutionContext`.

### 2.4 Activity Model

All activities implement `IActivity`. Composite activities that schedule children implement `IComposite` or derive from `Container`. Activities that can start a workflow from an external event implement `ITrigger`. Ports (named outcomes such as `"True"`, `"False"`, or `"Done"`) are declared via `[Port]` attributes or `[FlowNode]` on flowchart activities.

The built-in activity library covers:

| Category | Activities |
|---|---|
| Sequence/control flow | `Sequence`, `Flowchart`, `Fork`, `If`, `Switch`, `While`, `For`, `ForEach`, `Break`, `Parallel`, `ParallelForEach` |
| Flowchart-specific | `FlowDecision`, `FlowFork`, `FlowJoin`, `FlowSwitch` |
| I/O | `WriteLine`, `ReadLine`, `SetVariable`, `SetName`, `Correlate` |
| Lifecycle | `Start`, `End`, `Finish`, `Fault`, `Complete`, `Inline` |
| HTTP | `HttpEndpoint`, `SendHttpRequest`, `FlowSendHttpRequest`, `WriteHttpResponse`, `DownloadHttpFile`, `WriteFileHttpResponse` |
| Scheduling | `Timer`, `Cron`, `StartAt`, `Delay` |
| C# | `RunCSharp` |

### 2.5 Bookmark and Stimulus Model

Long-running workflows suspend by creating one or more `Bookmark` objects. A bookmark records the activity node, activity instance, a hashed payload key (the _stimulus hash_), optional callback method name, and `AutoBurn`/`AutoComplete` flags. The engine transitions the workflow to `WorkflowStatus.Running / WorkflowSubStatus.Suspended` when any bookmarks remain after the scheduler queue empties.

External events resume suspended workflows via the `IStimulusSender` service. `SendAsync` hashes the inbound stimulus, looks up matching bookmarks via `IBookmarkBoundWorkflowService`, and either resumes existing instances or triggers new ones if triggers match. This decouples the event source (HTTP endpoint, message consumer, scheduled timer, custom trigger) from the workflow host.

```csharp
// Activity creates a bookmark to suspend and wait for an external event
await context.CreateBookmarkAsync(new CreateBookmarkArgs
{
    Payload = new HttpEndpointBookmarkPayload(path, method),
    CallbackMethodName = nameof(HandleRequest)
});
```

### 2.6 Workflow Runtime Variants

| Runtime | Class | Use Case |
|---|---|---|
| **Local** | `LocalWorkflowRuntime` | Single-node; no distributed locking. Development and single-instance deployments only. |
| **Distributed** | `DistributedWorkflowRuntime` | Multi-node; wraps local execution in a `DistributedWorkflowClient` that acquires a `DistributedLock.Core` lock before running a workflow instance. Prevents concurrent execution of the same instance across pods. |

Both runtimes expose `IWorkflowRuntime`, which produces `IWorkflowClient` instances. The `DistributedBookmarkQueueWorker` additionally acquires a cluster-wide lock before processing the bookmark queue, preventing duplicate processing on multi-pod deployments. The `Medallion.Threading` library provides the underlying distributed lock abstraction.

### 2.7 Workflow State Serialization and Persistence

`WorkflowState` is the serializable snapshot of a running instance. It contains status, correlation, bookmarks, activity execution contexts (as `ActivityExecutionContextState`), variable values, incidents, and completion callbacks. The `IWorkflowStateExtractor` converts a live `WorkflowExecutionContext` to `WorkflowState` and back.

Persistence is organized into two store groups:

**Management stores** (workflow definitions and instances):
- `IWorkflowDefinitionStore` — CRUD for `WorkflowDefinition` entities (versioned, with `DefinitionId` + integer version)
- `IWorkflowInstanceStore` — CRUD for `WorkflowInstance` entities

**Runtime stores** (execution data):
- `IBookmarkStore`, `IBookmarkQueueStore`
- `IActivityExecutionStore`
- `IWorkflowExecutionLogStore`, `IActivityExecutionLogStore`
- `ITriggerStore`
- `IKeyValueStore`

All stores are interface-backed. Provided implementations:

| Backend | Module(s) |
|---|---|
| In-memory | Default in `Elsa.Workflows.Runtime` (lost on restart — not for production) |
| EF Core — SQLite | `Elsa.Persistence.EFCore.Sqlite` |
| EF Core — SQL Server | `Elsa.Persistence.EFCore.SqlServer` |
| EF Core — PostgreSQL | `Elsa.Persistence.EFCore.PostgreSql` |
| EF Core — MySQL | `Elsa.Persistence.EFCore.MySql` |
| EF Core — Oracle | `Elsa.Persistence.EFCore.Oracle` |
| Blob storage | `Elsa.WorkflowProviders.BlobStorage` (workflow definitions only, via FluentStorage) |

MongoDB and Dapper adapters referenced in the README and older documentation are not present in the `release/3.7.0` source tree. They may exist as third-party or commercial packages outside this repository.

### 2.8 Expression Engine

Elsa provides a pluggable expression evaluation system via `IExpressionHandler` implementations registered per language name. Each input on an activity accepts an `Expression` object with a `Type` discriminator (language name) and a `Value` payload.

| Language | Provider module | Engine |
|---|---|---|
| C# | `Elsa.Expressions.CSharp` | `Microsoft.CodeAnalysis.CSharp.Scripting` (Roslyn) |
| JavaScript | `Elsa.Expressions.JavaScript` | Jint 4.x |
| Python | `Elsa.Expressions.Python` | `pythonnet` 3.x (requires Python runtime on host) |
| Liquid | `Elsa.Expressions.Liquid` | Fluid.Core 2.x |
| ElsaScript DSL | `Elsa.Dsl.ElsaScript` | Custom regex-based parser + compiler |
| Literal / Delegate | `Elsa.Expressions` | Native .NET |

The JavaScript engine (Jint) supports optional CLR access (`AllowClrAccess`) and optional `getConfig` access for reading `IConfiguration` values; both are disabled by default for security. Python requires a Python runtime path configured via `PYTHONNET_PYDLL` or application settings.

---

## 3. Key Capabilities

### 3.1 Workflow Authoring

| Capability | Detail |
|---|---|
| Code-first C# | Derive from `WorkflowBase` or `WorkflowBase<TResult>`, implement `Build(IWorkflowBuilder)` |
| JSON definitions | Store `WorkflowDefinition` entities; loaded via `IWorkflowDefinitionStore` at runtime |
| ElsaScript text DSL | `.elsa` files; JavaScript-inspired syntax compiled to Elsa activity graphs |
| Activity host registration | Mark a CLR class as an activity host; public async methods are auto-discovered as activities via `HostMethodActivity` |
| Visual designer | Requires the separate Elsa Studio Blazor app |

### 3.2 Workflow Execution

| Capability | Detail |
|---|---|
| Synchronous in-process execution | `IWorkflowRunner.RunAsync` executes synchronously within the calling scope |
| Background dispatch | `IWorkflowDispatcher` queues execution to a background worker via `IBackgroundTaskDispatcher` |
| Parallel branches | `Fork`, `FlowFork`, `Parallel`, `ParallelForEach` schedule sibling activities concurrently within a single execution context |
| Long-running / suspended | Workflow suspends when bookmark queue drains; resumes on stimulus |
| Workflow versioning | `WorkflowDefinition` carries `DefinitionId` + integer `Version`; running instances track `DefinitionVersion` |
| Workflow import/export | `WorkflowDefinitionExporter` / `WorkflowDefinitionImporter`; bulk ZIP export with optional transitive consumer inclusion |
| Child workflows | `DispatchWorkflow` / `BulkDispatchWorkflows` activities; parent can wait for child completion |
| Instance activation strategies | `SingletonStrategy`, `CorrelationStrategy`, `CorrelatedSingletonStrategy` — prevent duplicate instances |
| Execution log | Per-activity execution records written via `IActivityExecutionStore`; configurable log persistence mode |
| Commit strategies | Configurable checkpointing: per-activity, per-workflow, or global defaults |

### 3.3 State Management

| Capability | Detail |
|---|---|
| Variable storage drivers | `WorkflowInstanceStorageDriver` (in-memory state); `WorkflowStorageDriver` (external store); extensible via `IStorageDriver` |
| Incident strategies | `FaultStrategy` (halt on first fault), `ContinueWithIncidentsStrategy` (record and continue) |
| Correlation | Workflows carry an optional `CorrelationId`; bookmark lookup uses correlation for routing |
| Heartbeat | `WorkflowHeartbeatGenerator` updates an `UpdatedAt` timestamp; `RestartInterruptedWorkflowsTask` uses inactivity thresholds to detect and restart stuck instances |

### 3.4 HTTP Integration

| Capability | Detail |
|---|---|
| `HttpEndpoint` trigger | Registers an ASP.NET Core route; creates a bookmark; resumes workflow on inbound request |
| `SendHttpRequest` | Outbound HTTP call; response properties accessible as activity output |
| `WriteHttpResponse` | Writes arbitrary response body/status to the current HTTP context |
| `DownloadHttpFile` | Downloads a remote file and returns a stream |
| SAS token support | `Elsa.SasTokens` module provides time-limited signed URLs for webhook callbacks |
| Resilience per request | HTTP activities participate in the `IResilientActivityInvoker` pipeline |

### 3.5 Scheduling

| Capability | Detail |
|---|---|
| `Timer` | Fires at a fixed interval; creates a bookmark on each tick |
| `Cron` | Fires on a Cronos cron expression schedule |
| `StartAt` | Fires once at a specific `DateTimeOffset` |
| `Delay` | Suspends the workflow for a fixed duration |
| Recurring background tasks | Configurable schedules per task type via `RecurringTaskOptions` |

### 3.6 Identity and Authentication

| Capability | Detail |
|---|---|
| JWT authentication | `IdentityTokenOptions` configures signing key, issuer, audience, access/refresh token lifetimes |
| API key authentication | `AspNetCore.Authentication.ApiKey` integration |
| Users | `IUserStore`, `IUserManager`, `IUserProvider`; configuration-based or store-backed providers |
| Applications | `IApplicationStore` for OAuth client credentials |
| Roles and permissions | `IRoleStore`, `IRoleManager`; default admin role uses wildcard permission (`"*"`) |
| Default admin bootstrap | `DefaultAdminUserFeature` creates a seeded admin user from `DefaultAdminUserOptions` if none exists |

### 3.7 Multi-Tenancy

| Capability | Detail |
|---|---|
| Tenant store | `ITenantStore` with `ConfigurationTenantsProvider` (appsettings) and `StoreTenantsProvider` (EF Core) |
| Tenant resolution pipeline | Pluggable pipeline via `ITenantResolutionPipeline`; resolvers execute in order until one resolves |
| Built-in HTTP resolvers | `HeaderTenantResolver`, `HostTenantResolver`, `RoutePrefixTenantResolver` |
| Identity resolvers | `ClaimsTenantResolver` (JWT claim), `CurrentUserTenantResolver` (user record lookup) |
| Tenant isolation | EF Core query filters include `TenantId`; empty string = default tenant; `null` = tenant-agnostic |
| ASP.NET Core middleware | `Elsa.Tenants.AspNetCore` provides `TenantResolutionMiddleware` |

### 3.8 Resilience

| Capability | Detail |
|---|---|
| Activity-level retry | Activities implement `IResilientActivity`; `IResilientActivityInvoker` wraps execution in a Polly `ResiliencePipeline<T>` |
| Strategy catalog | `IResilienceStrategyCatalog` with `IResilienceStrategySource` implementations |
| Configuration-backed strategies | `ConfigurationResilienceStrategySource` reads from `appsettings.json` under `Resilience:Strategies` |
| Transient exception detection | `ITransientExceptionDetector` / `DefaultTransientExceptionStrategy` |
| Retry attempt recording | `IRetryAttemptRecorder` stores attempt history into `ActivityExecutionContext` |
| Distributed lock resilience | `Elsa.Common` wraps distributed lock acquisition in a Polly retry pipeline |

### 3.9 Alterations API

| Alteration type | Effect |
|---|---|
| `Cancel` | Cancels all workflow instances in the alteration plan |
| `CancelActivity` | Cancels a specific running activity by activity ID or activity instance ID |
| `Migrate` | Migrates a workflow instance to a specified newer definition version |
| `ModifyVariable` | Overwrites a named variable's value in the running instance |
| `ScheduleActivity` | Forces a specific activity to be scheduled for execution |

### 3.10 Workflow Definition Management

| Capability | Detail |
|---|---|
| Definition versioning | `DefinitionId` (logical) + `Version` (integer); `IsPublished` and `IsLatest` flags |
| Import/export | ZIP archive with deterministic file names; optional recursive consumer inclusion |
| Consumer graph | `IWorkflowReferenceGraphBuilder` resolves all workflows that embed a given definition as a sub-workflow |
| Read-only mode | `UseReadOnlyMode(true)` disables mutation endpoints |
| Workflow providers | Extensible via `IWorkflowProvider`; blob storage provider available |

---

## 4. Integration Context

### 4.1 Inbound Triggers

| Source | Mechanism |
|---|---|
| HTTP request | `HttpEndpoint` activity; ASP.NET Core route registered at startup |
| Scheduled timer | `Timer`, `Cron`, `StartAt`, `Delay` — self-managed via recurring tasks |
| External event / message | Custom `ITrigger` or `IStimulusSender.SendAsync` call from message consumer |
| Manual start via API | `POST /workflow-definitions/{id}/execute` via `Elsa.Workflows.Api` |
| Activity bookmark resume | `IStimulusSender` / `IWorkflowResumer` matching on stimulus hash |

### 4.2 Persistence Infrastructure

| Concern | Options |
|---|---|
| Relational databases | SQLite, SQL Server, PostgreSQL, MySQL, Oracle (all via EF Core) |
| Workflow definition files | Blob storage via FluentStorage (Azure Blob, local file, others) |
| Distributed locking | `DistributedLock.Core` abstraction; file-system provider (default), database providers available |

### 4.3 Messaging

MassTransit (`MassTransit`, `MassTransit.RabbitMQ`, `MassTransit.Azure.ServiceBus.Core`) is listed in `Directory.Packages.props`, indicating first-class support for RabbitMQ and Azure Service Bus. However, in the `release/3.7.0` source tree no MassTransit integration module is present under `src/modules`. MassTransit integration likely ships as a separate extension package or commercial add-on.

### 4.4 Observability

| Signal type | Integration |
|---|---|
| Distributed traces | OpenTelemetry (`OpenTelemetry.Extensions.Hosting`, OTLP exporter, ASP.NET Core / HTTP / SqlClient instrumentation) |
| Logs | `ILogger<T>` throughout; Serilog integration available |
| APM | Datadog APM (`Datadog.Trace.Bundle` 3.32.0) in `Directory.Packages.props`; separate Dockerfile and Docker Compose for Datadog |

### 4.5 Reference Server and Docker

`Elsa.Server.Web` is the reference ASP.NET Core server. Docker Compose files (`docker/docker-compose.yml`) provide pre-configured containers for PostgreSQL, SQL Server, MySQL, Oracle, MongoDB, RabbitMQ, Redis, and SMTP4Dev. A load balancer reference app (`Elsa.Server.LoadBalancer`) demonstrates YARP-based multi-instance deployment.

---

## 5. Known Limitations

**Designer / Studio gaps:**
- The Elsa Studio designer supports only Flowchart activities. `Sequence` and `StateMachine` composite types cannot be designed visually; they are code-only or JSON-only.
- Starting a workflow from the designer is restricted to trigger-free, input-free workflows.
- UI input validation in the designer is not implemented.
- The workflow instance viewer does not yet render input/output values.

**Runtime:**
- `LocalWorkflowRuntime` has no cluster safety. Must not be used on multi-pod deployments without switching to `DistributedWorkflowRuntime`.
- The distributed runtime uses distributed locks to serialize execution per workflow instance. High-throughput scenarios with many concurrent short-running instances may experience lock contention.
- Heartbeat-based interrupted workflow detection relies on polling (`RestartInterruptedWorkflowsTask`). There is no push-based dead-instance detection.
- Near-real-time resumption under load depends on correct tuning of `TriggerBookmarkQueueRecurringTask` polling schedules.

**Persistence:**
- MongoDB and Dapper adapters are referenced in documentation but are absent from the `release/3.7.0` source tree.
- In-memory stores (the default when no EF Core module is registered) do not survive process restarts and are unsuitable for production use.
- EF Core migrations must be applied before version upgrades that include schema changes.

**Expressions:**
- Python expressions require a native Python runtime installed on the host. Container deployments must include Python in the base image.
- Enabling `AllowClrAccess` for JavaScript expressions provides unrestricted .NET type access from within Jint scripts — a significant security risk for user-defined workflows.
- The ElsaScript DSL uses a regex-based parser, limiting its reliability for complex or nested syntax.

**Multi-tenancy:**
- Background jobs and scheduled tasks use a separate `TenantTaskManager` that must be explicitly wired; missing configuration causes tenant context loss during background execution.
- The tenant-ID convention changed in 3.6.0 (empty string for default, `null` for tenant-agnostic). Existing databases require migration before upgrading past 3.5.x.

**Security:**
- The identity module's `DefaultSecretHasher` uses SHA-256 for password hashing — not a key-derivation function. Production deployments using the built-in identity provider are vulnerable to offline dictionary attacks.
- The `DefaultAdminUserFeature` creates a seeded admin user with wildcard permissions. Leaving default credentials unchanged is a documented risk.
- The sample `appsettings.json` contains a default JWT signing key committed to source control; production deployments must override via secrets management.

**Observability:**
- Execution log records can grow very large for deeply nested workflows. No automatic archival or TTL is applied to execution log records.
- No first-party metrics dashboard or SLA tracking; operators must instrument via OpenTelemetry and external platforms.

**Roadmap gaps (as of 3.7.0):**
- No StateMachine visual designer support.
- No native MassTransit integration module in the `src/` tree.
- No MongoDB or Dapper persistence modules in the `src/` tree.
- OpenTelemetry Redis instrumentation is listed in dependencies but there is no built-in Elsa Redis workflow state store.
