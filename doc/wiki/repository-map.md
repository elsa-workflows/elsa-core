# Repository Map

Elsa Core is organized as a large multi-project .NET solution. The repo favors small, independently packaged modules over one monolith.

## Top-Level Layout

| Path | Purpose |
| --- | --- |
| [src/apps](../../src/apps) | Runnable reference hosts, load-balancer host, modular server host, and sample package. |
| [src/modules](../../src/modules) | Elsa product modules: workflow engine, runtime, management, APIs, HTTP, identity, persistence, diagnostics, scripting, scheduling, tenants, labels, resilience, and more. |
| [src/common](../../src/common) | Shared infrastructure such as feature/module plumbing, mediator, API helpers, and test helpers. |
| [src/clients](../../src/clients) | Client packages, currently including the Elsa API client. |
| [src/extensions](../../src/extensions) | Extension packages that are not core modules. |
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
| Workflow API | [Elsa.Workflows.Api](../../src/modules/Elsa.Workflows.Api) and [Elsa.Api.Common](../../src/common/Elsa.Api.Common) | FastEndpoints registration, workflow endpoints, real-time workflow updates, API serialization. |
| Expression languages | [Elsa.Expressions](../../src/modules/Elsa.Expressions), [CSharp](../../src/modules/Elsa.Expressions.CSharp), [JavaScript](../../src/modules/Elsa.Expressions.JavaScript), [Python](../../src/modules/Elsa.Expressions.Python), [Liquid](../../src/modules/Elsa.Expressions.Liquid) | Expression evaluation and language-specific activities/descriptors. |
| Transport/activity packages | [Elsa.Http](../../src/modules/Elsa.Http), [Elsa.Scheduling](../../src/modules/Elsa.Scheduling), [Elsa.Resilience](../../src/modules/Elsa.Resilience) | HTTP triggers and calls, scheduled triggers, resilience strategies. |
| Persistence | [Elsa.Persistence.EFCore](../../src/modules/Elsa.Persistence.EFCore), provider packages under `Elsa.Persistence.EFCore.*`, and structured-log persistence packages | EF Core stores and provider-specific configuration/migrations. |
| Security and tenancy | [Elsa.Identity](../../src/modules/Elsa.Identity), [Elsa.Tenants](../../src/modules/Elsa.Tenants), [Elsa.Tenants.AspNetCore](../../src/modules/Elsa.Tenants.AspNetCore), [Elsa.SasTokens](../../src/modules/Elsa.SasTokens) | Users, applications, roles, API keys, tenants, tenant-aware routing, SAS tokens. |
| Diagnostics | [Elsa.Diagnostics.StructuredLogs](../../src/modules/Elsa.Diagnostics.StructuredLogs), [Relational](../../src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Relational), [Sqlite](../../src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite) | Structured `ILogger` capture, live feed, REST/SignalR endpoints, in-memory and SQLite storage. |
| Shells and modular hosting | [Elsa.Shells.Api](../../src/modules/Elsa.Shells.Api), CShells-facing shell feature classes throughout modules | Runtime-configurable feature loading for modular hosts. |

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
