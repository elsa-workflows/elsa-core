# Elsa Roadmap

Last refreshed: 2026-05-19

This roadmap is a product direction document, not a fixed release calendar. Elsa is developed through a mix of core maintainer work, customer-funded work, and community contributions, so sequencing can change when real-world demand changes. The intent is stable: make Elsa the most productive, dependable, and extensible workflow platform for the .NET ecosystem.

## North Star

Elsa should feel like the natural workflow engine for .NET teams:

- productive enough for application developers to model real business processes quickly
- dependable enough for production operations, rolling deploys, multi-tenant systems, and distributed workloads
- open enough to embed, customize, extend, and automate without fighting the framework
- powerful enough to connect systems, run long-lived processes, expose human tasks, and observe what is happening

## Current Foundations

These are already present in the codebase and should be treated as foundations for the next roadmap slices:

- Multi-targeting for `net8.0`, `net9.0`, and `net10.0` in [`src/Directory.Build.props`](src/Directory.Build.props).
- Modular core packages under [`src/modules`](src/modules), with code-first features and CShells shell features documented in [`doc/wiki/module-system.md`](doc/wiki/module-system.md).
- A modular server host using CShells and Nuplane package loading in [`src/apps/Elsa.ModularServer.Web`](src/apps/Elsa.ModularServer.Web).
- Runtime admin, quiescence, drain, and interrupted recovery infrastructure in [`Elsa.Workflows.Runtime`](src/modules/Elsa.Workflows.Runtime) and runtime admin endpoints in [`Elsa.Workflows.Api`](src/modules/Elsa.Workflows.Api/Endpoints/RuntimeAdmin).
- Distributed runtime support in [`Elsa.Workflows.Runtime.Distributed`](src/modules/Elsa.Workflows.Runtime.Distributed).
- Structured diagnostics with recent/live capture plus SQLite persistence in [`Elsa.Diagnostics.StructuredLogs`](src/modules/Elsa.Diagnostics.StructuredLogs) and [`Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite`](src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite).
- Raw stdout/stderr console diagnostics in [`Elsa.Diagnostics.ConsoleLogs`](src/modules/Elsa.Diagnostics.ConsoleLogs).
- State machine core activity support in [`Elsa.Workflows.Core/Activities/StateMachine`](src/modules/Elsa.Workflows.Core/Activities/StateMachine).
- ElsaScript DSL and blob storage integration in [`Elsa.Dsl.ElsaScript`](src/modules/Elsa.Dsl.ElsaScript) and [`Elsa.WorkflowProviders.BlobStorage.ElsaScript`](src/modules/Elsa.WorkflowProviders.BlobStorage.ElsaScript).
- Activity unit testing helpers and guidance in [`src/common/Elsa.Testing.Shared`](src/common/Elsa.Testing.Shared) and [`doc/qa/test-guidelines.md`](doc/qa/test-guidelines.md).
- Label infrastructure in [`Elsa.Labels`](src/modules/Elsa.Labels), which is the likely backend foundation for workflow categories, tags, and folders.

The public roadmap issue remains useful history: [elsa-workflows/elsa-core#3232](https://github.com/elsa-workflows/elsa-core/issues/3232). Several items in that issue are now done in code but still open in the issue body, so this file should be considered the current working roadmap.

## 1. Production Confidence

**Goal:** Operators should trust Elsa during deploys, restarts, scale-out, tenancy changes, and partial failures.

High-value items:

- Complete the graceful-shutdown operational slice: back-pressure-aware bookmark queueing, health checks, pause persistence across reactivation, and contract tests. The remaining task list is visible in [`specs/002-graceful-shutdown/tasks.md`](specs/002-graceful-shutdown/tasks.md).
- Close the workflow recovery story around interrupted, crashed, and stuck-running instances. This directly addresses [#4833](https://github.com/elsa-workflows/elsa-core/issues/4833) and should include Studio-facing recovery states, operator actions, and clear audit records.
- Harden distributed execution semantics: child workflow completion, bookmark races, duplicate dispatch, timer/delay behavior, and clustered refresh/reload. Community signal shows this repeatedly in [discussion #5857](https://github.com/elsa-workflows/elsa-core/discussions/5857), [#7397](https://github.com/elsa-workflows/elsa-core/issues/7397), [#7405](https://github.com/elsa-workflows/elsa-core/issues/7405), and related FlowJoin/bookmark issues.
- Turn the draft native background execution architecture into an implementation plan. [#7356](https://github.com/elsa-workflows/elsa-core/issues/7356) and [#7313](https://github.com/elsa-workflows/elsa-core/issues/7313) point toward an engine-owned, workflow-aware runtime that can evolve toward an actor-model abstraction without coupling Elsa to Orleans, Proto.Actor, or any single backend.
- Treat persistence and migration reliability as a product feature: provider-specific migration validation, large-tenant performance tests, safer defaults, and upgrade notes that cover SQL Server, PostgreSQL, MySQL, SQLite, Oracle, and MongoDB scenarios.
- Maintain a security hardening track: document scripting trust boundaries, production-safe Docker posture, OIDC setup, default auth patterns, and secrets redaction. [#7096](https://github.com/elsa-workflows/elsa-core/issues/7096) is a reminder that optional code execution must be explained and guarded clearly.

Recommended success measures:

- rolling deploys do not leave workflows ambiguous or unrecoverable
- every runtime admin action is observable and auditable
- core distributed scenarios have component tests, not only unit coverage
- upgrade guides call out migrations, package renames, and production security implications before users hit them

## 2. Authoring Productivity

**Goal:** Developers and business users should be able to create, understand, test, and change workflows without ceremony.

High-value items:

- Ship workflow organization as a coherent feature: labels/categories, folder-like views, search/filter by metadata, and Studio support. This consolidates [#5872](https://github.com/elsa-workflows/elsa-core/issues/5872), [#6307](https://github.com/elsa-workflows/elsa-core/issues/6307), the existing `Elsa.Labels` module, and workflow definition `CustomProperties`.
- Make workflow progress visible to application users: a current-state/step API, timeline model, and embeddable progress component. This responds to [discussion #6012](https://github.com/elsa-workflows/elsa-core/discussions/6012) and should reuse execution logs, activity records, call-stack tracking, and real-time workflow updates.
- Finish the state machine product surface. The core activity exists, but [#5085](https://github.com/elsa-workflows/elsa-core/issues/5085) should be closed only when JSON serialization, Studio authoring, docs, and examples make state machines approachable.
- Build first-class workflow testing and debugging: test runners for full workflows, breakpoint-like inspection, replay from execution logs where feasible, better failed-activity retry flows, and Studio affordances for fault investigation. This expands the activity test helper work that addressed [#3978](https://github.com/elsa-workflows/elsa-core/issues/3978).
- Improve designer extensibility and embedding: custom activity property editors, custom list actions, embeddable designer/viewer recipes, and clear Blazor/WASM/Server guidance. Community demand appears in [#4743](https://github.com/elsa-workflows/elsa-core/issues/4743), [#6685](https://github.com/elsa-workflows/elsa-core/issues/6685), [discussion #7246](https://github.com/elsa-workflows/elsa-core/discussions/7246), and older designer issues.
- Promote ElsaScript from experiment to useful authoring path: stable syntax, import/export round-tripping, editor diagnostics, examples, and clear boundaries with JSON and visual authoring. See [#7055](https://github.com/elsa-workflows/elsa-core/issues/7055) and the current [`Elsa.Dsl.ElsaScript`](src/modules/Elsa.Dsl.ElsaScript) module.

Recommended success measures:

- teams with 100+ workflow definitions can find and govern them without naming hacks
- a developer can unit test an activity, integration test a workflow, and debug a failed instance from documented recipes
- common Studio customization no longer requires replacing entire pages or reverse engineering internals

## 3. Integrations And Ecosystem

**Goal:** Elsa should make external systems feel like native workflow building blocks.

High-value items:

- Create an OpenAPI activity provider that turns an OpenAPI document into typed designer activities. This is a recurring ask in [#2961](https://github.com/elsa-workflows/elsa-core/issues/2961) and [#6360](https://github.com/elsa-workflows/elsa-core/issues/6360), and it is the best foundation for a broad connector story.
- Define a connector SDK before adding many one-off integrations. The SDK should cover authentication, secrets, generated activities, testing, metadata, versioning, and packaging.
- Continue marketplace and plugin infrastructure. The Nuplane modular server, sample package, package manifest metadata, and [#7310](https://github.com/elsa-workflows/elsa-core/issues/7310) point to a compelling future where Elsa distributions can install safe custom modules without users maintaining a separate host app.
- Resolve the MassTransit strategy after the v9 licensing change. [discussion #6583](https://github.com/elsa-workflows/elsa-core/discussions/6583) raises a practical ecosystem risk; Elsa should either provide a clean split or reduce dependency weight through a smaller messaging abstraction.
- Clarify Azure Functions and worker-service hosting patterns. [discussion #4707](https://github.com/elsa-workflows/elsa-core/discussions/4707) and [discussion #7420](https://github.com/elsa-workflows/elsa-core/discussions/7420) show demand for non-traditional hosts, Windows services, and serverless-adjacent deployments.
- Add data movement and streaming workflow primitives. [#4809](https://github.com/elsa-workflows/elsa-core/issues/4809) frames this as datasets, linked services, transforms, and stream-oriented processing inspired by Azure Data Factory and stream analytics.
- Treat BPMN as interoperability first, not a wholesale product pivot. [#39](https://github.com/elsa-workflows/elsa-core/issues/39) has strong interest, but the pragmatic first slice is import/export or a constrained BPMN compatibility layer, not full BPMN engine parity.

Recommended success measures:

- a team can connect to a REST API from its OpenAPI spec without hand-building HTTP activities
- marketplace packages can declare features, dependencies, settings, infrastructure needs, and security posture
- integrations are tested with the same rigor as core modules, not shipped as opaque examples

## 4. Observability And Operations

**Goal:** Elsa should be easy to inspect from Studio and from standard production telemetry stacks.

High-value items:

- Finish the diagnostics trilogy: structured logs, console logs, and an explicit OpenTelemetry boundary. Structured and console logs now exist; OpenTelemetry traces/metrics need a current module story because [PR #5810](https://github.com/elsa-workflows/elsa-core/pull/5810) previously introduced a module, [#5988](https://github.com/elsa-workflows/elsa-core/issues/5988) asks for default metrics, and the current repo no longer contains an `Elsa.OpenTelemetry` module.
- Add default workflow semantic metrics: started, resumed, suspended, faulted, completed, active, activity executed/faulted, queue depth, recovery count, drain count, and dispatch latency. Align with OpenTelemetry semantic convention work where possible.
- Build Studio diagnostics pages that are useful under pressure: live console, structured logs, trace/metric links, workflow incident timelines, source health, dropped-event counters, and export/copy affordances.
- Make execution history easier to reason about: distinguish faulted, interrupted, cancelled, crash-recovered, retried, and operator-modified workflows consistently across API, Studio, logs, and metrics.

Recommended success measures:

- local developers can troubleshoot from Studio without opening server logs
- production operators can answer "what is stuck, why, and what changed?" from first-party telemetry
- diagnostic modules have safe redaction defaults and bounded memory behavior

## 5. Security, Identity, And Enterprise Readiness

**Goal:** Elsa should be straightforward to secure in real enterprise hosts.

High-value items:

- Publish canonical OIDC recipes for Blazor Server, WASM, separate server/studio, and all-in-one hosts. [#7181](https://github.com/elsa-workflows/elsa-core/issues/7181) shows both implementation and documentation demand.
- Provide a production security guide: API keys, JWT/OIDC, default admin bootstrap, scripting trust levels, C# expression risks, Docker demo boundaries, secret masking, tenant isolation, and permission design.
- Expand authorization coverage tests around workflow instances, runtime admin, diagnostics, labels, tenants, and HTTP endpoint activities.
- Improve multi-tenant ergonomics: tenant-agnostic workflows, high tenant counts, tenant validation modes, cache isolation, and clear migration guidance after the 3.6 tenant ID convention changes.
- Create an enterprise deployment checklist for Kubernetes, reverse proxies/base paths, TLS/custom CAs, database migrations, health checks, backups, and disaster recovery.

Recommended success measures:

- a new enterprise adopter can stand up Elsa behind their identity provider from docs alone
- optional dangerous capabilities are explicit opt-ins with plain-language warnings
- tenant and permission regressions are caught by targeted tests before release

## 6. AI-Assisted Workflow Engineering

**Goal:** AI should make workflows more transparent, not less.

High-value items:

- Build AI-assisted workflow generation that produces multiple visible activities from intent rather than hiding logic in one script activity. This direction is proposed in [discussion #7367](https://github.com/elsa-workflows/elsa-core/discussions/7367).
- Provide an Elsa MCP/tooling surface for reading, validating, editing, and explaining workflow JSON/ElsaScript. This would make Elsa a strong fit for AI-enabled .NET development environments.
- Add "explain this workflow", "find risky activities", "suggest tests", and "generate migration notes" capabilities backed by workflow graph metadata.
- Pair AI generation with validation: generated workflows should include test scaffolds, required input/output definitions, secrets handling, and clear review diffs.

Recommended success measures:

- AI-generated workflows remain inspectable in Studio
- generated workflows come with runnable tests or validation plans
- teams can use AI to refactor workflows without losing visibility, auditability, or review discipline

## Recommended Sequencing

Near term:

1. Finish runtime confidence work: graceful shutdown remaining tasks, recovery clarity, distributed runtime regressions, security documentation, and OIDC recipes.
2. Make workflow authoring less painful: organization, search, progress/timeline APIs, testing docs, and state machine Studio/docs completion.
3. Publish the updated roadmap and keep release discussions/milestones linked from it so users can plan adoption.

Mid term:

1. OpenAPI activity provider plus connector SDK.
2. Marketplace/plugin installation path built on Nuplane, shell features, and package manifests.
3. OpenTelemetry module boundary and default workflow metrics.
4. Workflow debugging and replay-oriented incident analysis.

Longer term:

1. Native workflow-aware background execution and actor-runtime abstraction.
2. Data pipeline/stream processing primitives.
3. BPMN interoperability.
4. AI-assisted authoring and workflow MCP tools.

## Maintainership Recommendations

- Keep this roadmap in source control and mirror major changes to [#3232](https://github.com/elsa-workflows/elsa-core/issues/3232). Users are explicitly asking for roadmap visibility in [discussion #7202](https://github.com/elsa-workflows/elsa-core/discussions/7202) and the latest roadmap issue comment.
- Use labels or milestones to connect issues to these roadmap themes. Many high-value issues are currently unlabeled, which makes demand hard to see.
- Close or update stale roadmap items that are already implemented, especially state machine, activity testing, ElsaScript, diagnostics, and graceful shutdown foundations.
- Prefer platform primitives over one-off features: connector SDK before many connectors, plugin system before bespoke managed extensibility, runtime abstraction before framework-specific actor work.
- Do not hide reliability work under "maintenance". Recovery, distributed correctness, migrations, security posture, and observability are core product features for a workflow engine.
