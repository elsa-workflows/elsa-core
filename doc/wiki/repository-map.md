# Repository Map

Elsa Core is organized as a large multi-project .NET solution. The repo favors small, independently packaged modules over one monolith.

## Top-Level Layout

| Path | Purpose |
| --- | --- |
| [src/apps](../../src/apps) | Runnable reference hosts, load-balancer host, modular server host, and sample package. |
| [src/modules](../../src/modules) | Elsa product modules: workflow engine, runtime, management, APIs, HTTP, identity, persistence, diagnostics, scripting, scheduling, tenants, labels, resilience, and more. |
| [src/common](../../src/common) | Shared infrastructure such as feature/module plumbing, mediator, API helpers, and test helpers. |
| [src/clients](../../src/clients) | Client packages, currently including the Elsa API client. |
| [src/extensions](../../src/extensions) | Extension packages that are not core modules. Currently contains [Elsa.Testing.Extensions](../../src/extensions/Elsa.Testing.Extensions). |
| [test/unit](../../test/unit) | Fast unit tests scoped to individual modules or services. |
| [test/integration](../../test/integration) | In-process tests that compose multiple Elsa services. |
| [test/component](../../test/component) | Larger host-level and persistence-oriented scenarios. |
| [test/performance](../../test/performance) | Benchmark and throughput-oriented tests. |
| [build](../../build) | NUKE build project and CI build wiring. |
| [doc](../../doc) | ADRs, QA notes, agent logs, bounty docs, and this wiki. |
| [specs](../../specs) | Spec Kit feature specs, plans, tasks, contracts, and quickstarts. |
| [design](../../design) | Logos, screenshots, and visual assets used by public docs and README files. |

## Major Module Families

| Family | Projects | What they own |
| --- | --- | --- |
| Base host package | [Elsa](../../src/modules/Elsa) | `AddElsa`, `ElsaFeature`, default workflow feature wiring. |
| Workflow engine | [Elsa.Workflows.Core](../../src/modules/Elsa.Workflows.Core) | Activities, execution contexts, pipelines, serialization, variables, bookmarks, graphs, flowchart primitives. |
| Workflow management | [Elsa.Workflows.Management](../../src/modules/Elsa.Workflows.Management) | Definitions, instances, stores, import/export, materializers, validation, descriptors. |
| Workflow runtime | [Elsa.Workflows.Runtime](../../src/modules/Elsa.Workflows.Runtime) and [Elsa.Workflows.Runtime.Distributed](../../src/modules/Elsa.Workflows.Runtime.Distributed) | Dispatch, triggers, bookmark queues, runtime logs, background activity scheduling, recovery, distributed runtime support. |
| Alterations | [Elsa.Alterations](../../src/modules/Elsa.Alterations), [Elsa.Alterations.Core](../../src/modules/Elsa.Alterations.Core) | Bulk alteration of running workflow instances: alteration plans, jobs, dispatching, and in-memory stores; EF Core and vNext persistence packages live alongside the core. |
| Workflow API | [Elsa.Workflows.Api](../../src/modules/Elsa.Workflows.Api) and [Elsa.Api.Common](../../src/common/Elsa.Api.Common) | FastEndpoints registration, workflow endpoints, real-time workflow updates, API serialization. |
| Expression languages | [Elsa.Expressions](../../src/modules/Elsa.Expressions), [CSharp](../../src/modules/Elsa.Expressions.CSharp), [JavaScript](../../src/modules/Elsa.Expressions.JavaScript), [Python](../../src/modules/Elsa.Expressions.Python), [Liquid](../../src/modules/Elsa.Expressions.Liquid) | Expression evaluation and language-specific activities/descriptors. |
| Transport/activity packages | [Elsa.Http](../../src/modules/Elsa.Http), [Elsa.Http.Webhooks](../../src/modules/Elsa.Http.Webhooks), [Elsa.Scheduling](../../src/modules/Elsa.Scheduling), [Elsa.Resilience](../../src/modules/Elsa.Resilience) | HTTP triggers and calls, outbound webhook sinks and activity-driven webhook sources (`WebhooksFeature`), scheduled triggers, resilience strategies. |
| BPMN | [Elsa.Bpmn](../../src/modules/Elsa.Bpmn), [Elsa.Bpmn.Interchange](../../src/modules/Elsa.Bpmn.Interchange) | BPMN 2.0 process execution: `BpmnProcess` scope activity, work ledger, scope signals; and the `elsa:` XML interchange format that binds BPMN document elements to Elsa activities. See [bpmn-workflows.md](bpmn-workflows.md). |
| Persistence (EF Core) | [Elsa.Persistence.EFCore](../../src/modules/Elsa.Persistence.EFCore), provider packages under `Elsa.Persistence.EFCore.*`, and structured-log persistence packages | EF Core stores and provider-specific configuration/migrations. |
| Persistence vNext | [Elsa.Persistence.VNext](../../src/modules/Elsa.Persistence.VNext), [Extensions](../../src/modules/Elsa.Persistence.VNext.Extensions), [Runtime](../../src/modules/Elsa.Persistence.VNext.Runtime), [Relational](../../src/modules/Elsa.Persistence.VNext.Relational), [Sqlite](../../src/modules/Elsa.Persistence.VNext.Sqlite), [PostgreSql](../../src/modules/Elsa.Persistence.VNext.PostgreSql), [SqlServer](../../src/modules/Elsa.Persistence.VNext.SqlServer), [MongoDb](../../src/modules/Elsa.Persistence.VNext.MongoDb) | Next-generation provider-neutral persistence: module-owned storage manifests, portable document/index store, schema versioning, and physicalization for relational and document databases. |
| Key-value store | [Elsa.KeyValues](../../src/modules/Elsa.KeyValues) | Generic key-value storage (`IKeyValueStore`) with a default in-memory backing store; used by other modules for ephemeral or cross-request state. |
| Caching | [Elsa.Caching](../../src/modules/Elsa.Caching) | `ICacheManager` and `IChangeTokenSignaler`; provides memory-cache helpers and signal-based cache invalidation used internally by other Elsa modules. |
| Security and tenancy | [Elsa.Identity](../../src/modules/Elsa.Identity), [Elsa.Tenants](../../src/modules/Elsa.Tenants), [Elsa.Tenants.AspNetCore](../../src/modules/Elsa.Tenants.AspNetCore), [Elsa.SasTokens](../../src/modules/Elsa.SasTokens) | Users, applications, roles, API keys, tenants, tenant-aware routing, SAS tokens. |
| External authentication | [Elsa.ExternalAuthentication](../../src/modules/Elsa.ExternalAuthentication), [Elsa.ExternalAuthentication.OpenIdConnect](../../src/modules/Elsa.ExternalAuthentication.OpenIdConnect), [Elsa.ExternalAuthentication.Secrets](../../src/modules/Elsa.ExternalAuthentication.Secrets), and EF Core provider packages (`Sqlite`, `SqlServer`, `PostgreSql`, `MySql`, `Oracle`) | Server-brokered external identity providers: Identity Provider Connections, OpenID Connect adapter, linked identity resolution, configurable unlinked-identity policies, Elsa credential issuance, and EF Core persistence. See [specs/012-external-authentication/spec.md](../../specs/012-external-authentication/spec.md). |
| User Tasks | [Elsa.UserTasks](../../src/modules/Elsa.UserTasks), [Elsa.UserTasks.Persistence.EFCore](../../src/modules/Elsa.UserTasks.Persistence.EFCore) and provider packages (`Sqlite`, `SqlServer`, `PostgreSql`, `MySql`, `Oracle`), [Elsa.UserTasks.Persistence.VNext](../../src/modules/Elsa.UserTasks.Persistence.VNext) | Durable, identity-neutral, workflow-bound human tasks: task queue with lifecycle (Available, Assigned, Completing, Completed, Cancelled, TimedOut), forms, guest invitations, due-date handling, hosted projector, reconciler, and delivery workers. See [user-tasks.md](user-tasks.md). |
| Secrets | [Elsa.Secrets](../../src/modules/Elsa.Secrets), [Elsa.Secrets.Persistence.EFCore](../../src/modules/Elsa.Secrets.Persistence.EFCore), [Elsa.Secrets.Persistence.VNext](../../src/modules/Elsa.Secrets.Persistence.VNext), [Elsa.Secrets.JavaScript](../../src/modules/Elsa.Secrets.JavaScript) | Named secrets with pluggable stores, extensible secret types (text, RSA key, X.509 certificate), versioning, rotation, revocation, secret resolver, management endpoints, EF Core and vNext persistence, and JavaScript expression access. |
| Diagnostics | [Elsa.Diagnostics.StructuredLogs](../../src/modules/Elsa.Diagnostics.StructuredLogs), [Relational](../../src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational), [Sqlite](../../src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite), [Elsa.Diagnostics.ConsoleLogs](../../src/modules/Elsa.Diagnostics.ConsoleLogs) | Structured `ILogger` capture, raw console capture, live feed, REST/SignalR endpoints, in-memory and SQLite storage. |
| Shells and modular hosting | [Elsa.Shells.Api](../../src/modules/Elsa.Shells.Api), CShells-facing shell feature classes throughout modules | Runtime-configurable feature loading for modular hosts. |
| Operational dashboard | [Elsa.Dashboard.Api](../../src/modules/Elsa.Dashboard.Api) | Read-only aggregate endpoints for the Studio operational dashboard: overview, trends, needs-attention findings, recent activity, and workflow hotspots. |
| Application clustering | [Elsa.Hosting.Management](../../src/modules/Elsa.Hosting.Management) | Application instance naming, heartbeat-based cluster membership, and instance-aware hosted service support for multi-node deployments. |
| AI / Weaver | [Elsa.AI.Abstractions](../../src/modules/Elsa.AI.Abstractions), [Elsa.AI.Copilot](../../src/modules/Elsa.AI.Copilot), [Elsa.AI.Host](../../src/modules/Elsa.AI.Host), [Elsa.AI.Persistence.EFCore](../../src/modules/Elsa.AI.Persistence.EFCore) | Weaver AI copilot: streaming chat, context resolution, reviewable proposals, audit, and provider abstraction for AI-assisted workflow authoring. |

## Reference Hosts

- [Elsa.Server.Web](../../src/apps/Elsa.Server.Web) is the most useful all-up ASP.NET Core sample. Its [Program.cs](../../src/apps/Elsa.Server.Web/Program.cs) shows typical module composition with identity, EF Core SQLite, runtime, management, HTTP, scheduling, scripting, multitenancy, and optional structured logs.
- [Elsa.ModularServer.Web](../../src/apps/Elsa.ModularServer.Web) demonstrates modular package loading through Nuplane and shell features.
- [Elsa.Server.LoadBalancer](../../src/apps/Elsa.Server.LoadBalancer) is a load-balancer host.
- [Elsa.SamplePackage](../../src/apps/Elsa.SamplePackage) is a minimal package-style feature sample.

## Build And Package Files

- [Directory.Build.props](../../Directory.Build.props) contains shared MSBuild settings.
- [src/Directory.Build.props](../../src/Directory.Build.props) multi-targets source packages for `net8.0`, `net9.0`, and `net10.0`.
- [Directory.Packages.props](../../Directory.Packages.props) centrally manages package versions, including conditional versions for .NET 8/9 versus .NET 10.
- [build/Build.cs](../../build/Build.cs) defines the NUKE build, test, and package targets.

## How To Find Code Fast

Use the feature class first. Most modules have a `Features/*Feature.cs` and often a parallel `ShellFeatures/*Feature.cs`. The feature class tells you what the module registers and what other features it depends on. After that, follow contracts and service registrations into implementation files.

Good first searches:

```bash
rg "class .*Feature" src/modules src/common
rg "interface I.*Store" src/modules
rg "AddScoped|AddSingleton|TryAdd" src/modules/Elsa.Workflows.Runtime/Features
rg "Get\\(|Post\\(|Delete\\(" src/modules/Elsa.Workflows.Api/Endpoints
```
