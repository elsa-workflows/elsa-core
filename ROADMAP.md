# Elsa Roadmap

Last refreshed: 2026-06-24

This roadmap is a product direction document, not a fixed release calendar. Elsa is developed through a mix of core maintainer work, customer-funded work, and community contributions, so sequencing can change when real-world demand changes. The intent is stable: make Elsa the most productive, dependable, and extensible workflow platform for the .NET ecosystem.

This version is grounded in the current `elsa-core`, `elsa-studio`, and `elsa-extensions` repositories, including source code, open issues, open pull requests, releases, and discussions where available.

## North Star

Elsa should feel like the natural workflow engine for .NET teams:

- productive enough for application developers to model real business processes quickly
- dependable enough for production operations, rolling deploys, multi-tenant systems, and distributed workloads
- open enough to embed, customize, extend, and automate without fighting the framework
- powerful enough to connect systems, run long-lived processes, expose human tasks, and observe what is happening

## Capability Checklist

Legend: `[x]` shipped foundation, `[~]` partially shipped or needs productization, `[ ]` roadmap candidate.

### Engine

- [x] Non-blocking activity execution
- [x] Fork, join, and explicit flowchart merge modes
- [x] State machine core activity
- [~] State machine Studio authoring, docs, and examples
- [~] Graceful shutdown and interrupted recovery
- [ ] Full workflow execution recovery UX
- [ ] Compensation / saga support
- [ ] Variable history tracking
- [ ] Native workflow-aware background execution
- [ ] Actor-runtime abstraction

### Runtime And Operations

- [x] Runtime admin pause, resume, status, and force-drain endpoints
- [x] Distributed runtime package
- [x] Structured logs
- [x] Console logs
- [x] Studio structured-log, console-log, and OpenTelemetry diagnostics foundations
- [x] Durable structured log persistence
- [~] Scheduler and message-bus foundations through Quartz, Hangfire, MassTransit, Kafka, and Azure Service Bus
- [~] OpenTelemetry diagnostics backend and default workflow metrics
- [ ] Scheduler/message reliability hardening for clustered production workloads
- [ ] Production security guide
- [ ] Enterprise deployment platform and checklist

### Authoring And Studio

- [x] Visual designer foundation
- [x] Modular Studio shell, feature system, menus, widgets, themes, localization, and remote feature gating
- [x] Studio custom elements for embedding definition editors, instance viewers, and workflow lists
- [x] Activity unit testing helpers
- [x] ElsaScript experimental DSL
- [~] React Flow, sequence, and state-machine designer foundations
- [~] Workflow organization with labels/categories/folders
- [~] Workflow progress/timeline surface
- [x] Studio OIDC and identity modules
- [~] Studio diagnostics pages for structured logs, console logs, and OpenTelemetry
- [~] Studio alterations module
- [ ] Designer reliability/regression hardening
- [ ] Input validation and activity version visibility
- [ ] Async dispatch/run UX
- [ ] User preferences, table state, and layout persistence
- [ ] First-class workflow debugging
- [ ] Designer extensibility, embedding, and white-label recipes
- [ ] Tenant/role-based activity visibility
- [~] AI-assisted workflow generation and copilot foundations

### Integrations

- [x] HTTP, scheduling, scripting, and persistence provider foundations
- [x] Connections and Secrets foundations
- [x] Extension packages for SQL, CSV, Email, Slack, Telnyx, GitHub DevOps, Azure Storage, Azure Service Bus, Kafka, MassTransit/RabbitMQ, Quartz, Hangfire, Dapper, MongoDB, Elasticsearch, OpenTelemetry, Logging, Agents, OpenAPI, Webhooks, OrchardCore, IO, compression, and ProtoActor-backed runtime/caching
- [~] Modular package loading and manifest metadata
- [~] OpenAPI activity/provider foundations
- [ ] Connector SDK
- [ ] Marketplace/plugin installation
- [~] Agent provider matrix, with MCP/tool lifecycle still roadmap
- [ ] Dynamic activity generation from OpenAPI, Azure Functions, registered methods, and schemas
- [ ] Azure DevOps, Teams, OneDrive, SharePoint, Google Docs, and Google Sheets integration strategy
- [ ] SQL authoring quality: Studio drag/drop reliability and IntelliSense
- [~] Dapper package split and MongoDB secrets parity
- [ ] MassTransit v9 strategy
- [ ] Azure Functions / worker-service hosting guidance
- [ ] WatchFileSystem and command-line automation activities
- [ ] Data pipeline / ETL primitives
- [ ] BPMN interoperability

## Current Foundations

These are already present in the codebase and should be treated as foundations for the next roadmap slices:

- Multi-targeting for `net8.0`, `net9.0`, and `net10.0` in [`src/Directory.Build.props`](src/Directory.Build.props).
- The `3.7.0` release train shipped across [Core](https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.0), [Studio](https://github.com/elsa-workflows/elsa-studio/releases/tag/3.7.0), and [Extensions](https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.7.0) in May 2026, promoting shell integration, Studio authentication, workflow diagnostics, and extension package metadata into released foundations. The [Core](https://github.com/elsa-workflows/elsa-core/releases/tag/3.8.0-preview1) and [Studio](https://github.com/elsa-workflows/elsa-studio/releases/tag/3.8.0-preview1) `3.8.0-preview1` releases on June 1, 2026 then added the next preview slice of graceful shutdown, richer diagnostics, secrets, and newer designer surfaces. The `3.7.1` patch train then shipped across [Core](https://github.com/elsa-workflows/elsa-core/releases/tag/3.7.1), [Studio](https://github.com/elsa-workflows/elsa-studio/releases/tag/3.7.1), and [Extensions](https://github.com/elsa-workflows/elsa-extensions/releases/tag/3.7.1) on June 21, 2026, tightening Azure Service Bus startup reliability, aligning Studio with the released Core API client, and hardening Quartz durability and endpoint-name pressure in Extensions.
- Modular core packages under [`src/modules`](src/modules), with code-first features and CShells shell features documented in [`doc/wiki/module-system.md`](doc/wiki/module-system.md).
- A modular server host using CShells and Nuplane package loading in [`src/apps/Elsa.ModularServer.Web`](src/apps/Elsa.ModularServer.Web).
- Runtime admin, quiescence, drain, and interrupted recovery infrastructure in [`Elsa.Workflows.Runtime`](src/modules/Elsa.Workflows.Runtime) and runtime admin endpoints in [`Elsa.Workflows.Api`](src/modules/Elsa.Workflows.Api/Endpoints/RuntimeAdmin).
- Distributed runtime support in [`Elsa.Workflows.Runtime.Distributed`](src/modules/Elsa.Workflows.Runtime.Distributed).
- Structured diagnostics with recent/live capture plus SQLite persistence in [`Elsa.Diagnostics.StructuredLogs`](src/modules/Elsa.Diagnostics.StructuredLogs) and [`Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite`](src/modules/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite).
- Raw stdout/stderr console diagnostics in [`Elsa.Diagnostics.ConsoleLogs`](src/modules/Elsa.Diagnostics.ConsoleLogs), with the post-3.7 console pipeline now carrying workflow and activity execution context through [PR #7536](https://github.com/elsa-workflows/elsa-core/pull/7536).
- Core `main` now includes `Elsa.Diagnostics.OpenTelemetry`, which provides OTLP ingestion, bounded in-memory storage, REST APIs, SignalR live updates, collector configuration, permissions, and tests in [`src/modules/Elsa.Diagnostics.OpenTelemetry`](src/modules/Elsa.Diagnostics.OpenTelemetry). The productization gap is no longer "does a backend exist?" but rather release packaging, default workflow semantic metrics, and diagnostics correlation.
- Core `main` now includes `Elsa.AI.Abstractions`, `Elsa.AI.Host`, `Elsa.AI.Copilot`, and `Elsa.AI.Persistence.EFCore` through [PR #7523](https://github.com/elsa-workflows/elsa-core/pull/7523), and Studio `main` now includes `Elsa.Studio.AI` through [elsa-studio#900](https://github.com/elsa-workflows/elsa-studio/pull/900). That gives Weaver both server and Studio workspace foundations, while proposal actions, broader authoring contracts, and polished product UX remain roadmap work.
- State machine core activity support in [`Elsa.Workflows.Core/Activities/StateMachine`](src/modules/Elsa.Workflows.Core/Activities/StateMachine).
- ElsaScript DSL and blob storage integration in [`Elsa.Dsl.ElsaScript`](src/modules/Elsa.Dsl.ElsaScript) and [`Elsa.WorkflowProviders.BlobStorage.ElsaScript`](src/modules/Elsa.WorkflowProviders.BlobStorage.ElsaScript).
- Activity unit testing helpers and guidance in [`src/common/Elsa.Testing.Shared`](src/common/Elsa.Testing.Shared) and [`doc/qa/test-guidelines.md`](doc/qa/test-guidelines.md).
- Label infrastructure now spans Core and Studio: [`Elsa.Labels`](src/modules/Elsa.Labels) exposes label and workflow-label endpoints, and [`Elsa.Studio.Labels`](https://github.com/elsa-workflows/elsa-studio/tree/main/src/modules/Elsa.Studio.Labels) adds label management pages plus workflow-definition label editing. Folder views, broader metadata search, and richer organization UX remain roadmap work.
- Elsa Studio is already a modular Blazor product shell with workflow authoring, instance browsing, designer modules, diagnostics, authentication, localization, branding, custom elements, and early React wrapper work in [elsa-workflows/elsa-studio](https://github.com/elsa-workflows/elsa-studio).
- Studio `3.7.0` shipped the modern authentication framework, Elsa Identity and OIDC modules, activity call-stack visualization, incident count badges, pending-instance filtering, and custom theme/DataPanel extensibility.
- Studio `3.8.0-preview1` shipped the server logs module, console logs module, structured-log storage diagnostics, the OpenTelemetry diagnostics page from [elsa-studio#834](https://github.com/elsa-workflows/elsa-studio/pull/834), sequence and state-machine designer foundations, the secrets module, and the alterations designer.
- Elsa Extensions is an active modular integration repository with 70+ module projects in [elsa-workflows/elsa-extensions](https://github.com/elsa-workflows/elsa-extensions), targeting `net8.0`, `net9.0`, and `net10.0`.
- Extensions already provide broad integration foundations: Connections, Secrets, Agents, OpenAPI, SQL/CSV/data tooling, messaging, schedulers, cloud storage, logging, webhooks, persistence providers, LDAP, and external system activities.
- Extensions `3.7.0` adds package manifest metadata, infrastructure attributes, shell features for MassTransit/Quartz/Webhooks, Dapper and MongoDB activity execution-chain lookups, Dapper bookmark queue filtering, Kafka multitenancy/schema-trigger work, Quartz lifecycle/job cleanup fixes, and other operational hardening.

The public roadmap issue remains useful history: [elsa-workflows/elsa-core#3232](https://github.com/elsa-workflows/elsa-core/issues/3232). Several items in that issue are now done in code but still open in the issue body, so this file should be considered the current working roadmap.

## 1. Production Confidence

**Goal:** Operators should trust Elsa during deploys, restarts, scale-out, tenancy changes, and partial failures.

High-value items:

- Complete the graceful-shutdown operational slice: back-pressure-aware bookmark queueing, health checks, pause persistence across reactivation, and contract tests. The remaining task list is visible in [`specs/002-graceful-shutdown/tasks.md`](specs/002-graceful-shutdown/tasks.md).
- Close the workflow recovery story around interrupted, crashed, and stuck-running instances. This directly addresses [#4833](https://github.com/elsa-workflows/elsa-core/issues/4833) and should include Studio-facing recovery states, operator actions, and clear audit records.
- Harden distributed execution semantics: child workflow completion, bookmark races, duplicate dispatch, timer/delay behavior, and clustered refresh/reload. Community signal shows this repeatedly in [discussion #5857](https://github.com/elsa-workflows/elsa-core/discussions/5857), [#7397](https://github.com/elsa-workflows/elsa-core/issues/7397), [#7405](https://github.com/elsa-workflows/elsa-core/issues/7405), and related FlowJoin/bookmark issues.
- Treat scheduler and messaging correctness as release-blocking infrastructure. The new `3.7.1` patch line improved Azure Service Bus startup behavior and stable instance naming in Core ([#7732](https://github.com/elsa-workflows/elsa-core/issues/7732), [#7736](https://github.com/elsa-workflows/elsa-core/issues/7736), [#7742](https://github.com/elsa-workflows/elsa-core/pull/7742)) and tightened Quartz durable trigger scheduling in Extensions ([elsa-extensions#162](https://github.com/elsa-workflows/elsa-extensions/pull/162)). That progress is useful, but active issues around Quartz clustering and recovery ([elsa-extensions#109](https://github.com/elsa-workflows/elsa-extensions/issues/109), [elsa-extensions#101](https://github.com/elsa-workflows/elsa-extensions/issues/101)), Hangfire duplicate jobs ([elsa-extensions#121](https://github.com/elsa-workflows/elsa-extensions/issues/121)), MassTransit stimulus routing ([elsa-extensions#72](https://github.com/elsa-workflows/elsa-extensions/issues/72)), Kafka extensibility ([elsa-extensions#134](https://github.com/elsa-workflows/elsa-extensions/issues/134)), and Azure Service Bus backlog/startup pressure ([#7735](https://github.com/elsa-workflows/elsa-core/issues/7735), [#7737](https://github.com/elsa-workflows/elsa-core/issues/7737)) all point to the same production theme: clustered workload behavior must be boring, observable, and customizable.
- Turn the draft native background execution architecture into an implementation plan. [#7356](https://github.com/elsa-workflows/elsa-core/issues/7356) and [#7313](https://github.com/elsa-workflows/elsa-core/issues/7313) point toward an engine-owned, workflow-aware runtime that can evolve toward an actor-model abstraction without coupling Elsa to Orleans, Proto.Actor, or any single backend.
- Treat persistence and migration reliability as a product feature: provider-specific migration validation, large-tenant performance tests, safer defaults, and upgrade notes that cover SQL Server, PostgreSQL, MySQL, SQLite, Oracle, and MongoDB scenarios.
- Promote the Elsa Deployment Platform PRD into scoped implementation work. [#7469](https://github.com/elsa-workflows/elsa-core/issues/7469) defines the right product boundary: declarative environment manifests, immutable deployment artifacts, dry-run validation, deployment history, and GitOps-compatible reconciliation should manage control-plane state without reconciling runtime execution state.
- Maintain a security hardening track: document scripting trust boundaries, production-safe Docker posture, OIDC setup, default auth patterns, and secrets redaction. [#7096](https://github.com/elsa-workflows/elsa-core/issues/7096) is a reminder that optional code execution must be explained and guarded clearly.

Recommended success measures:

- rolling deploys do not leave workflows ambiguous or unrecoverable
- every runtime admin action is observable and auditable
- deployments can be previewed, validated, promoted, rolled back, and audited without copying databases or reconciling runtime state
- core distributed scenarios have component tests, not only unit coverage
- upgrade guides call out migrations, package renames, and production security implications before users hit them

## 2. Authoring Productivity

**Goal:** Developers and business users should be able to create, understand, test, and change workflows without ceremony.

High-value items:

- Ship workflow organization as a coherent feature: labels/categories, folder-like views, search/filter by metadata, and Studio support. This consolidates [#5872](https://github.com/elsa-workflows/elsa-core/issues/5872), [#6307](https://github.com/elsa-workflows/elsa-core/issues/6307), the existing `Elsa.Labels` module, and workflow definition `CustomProperties`.
- Make designer reliability a visible workstream. Recent Studio issues show expression/input rendering regressions after 3.6 ([elsa-studio#791](https://github.com/elsa-workflows/elsa-studio/issues/791), [elsa-studio#781](https://github.com/elsa-workflows/elsa-studio/issues/781), [elsa-studio#795](https://github.com/elsa-workflows/elsa-studio/issues/795)); these should drive a regression harness for designer rendering, property editors, expression descriptors, drag/drop, and WASM/Server parity.
- Make workflow progress visible to application users: a current-state/step API, timeline model, and embeddable progress component. This responds to [discussion #6012](https://github.com/elsa-workflows/elsa-core/discussions/6012) and should reuse execution logs, activity records, call-stack tracking, and real-time workflow updates.
- Finish the state machine product surface. The core activity exists, but [#5085](https://github.com/elsa-workflows/elsa-core/issues/5085) should be closed only when JSON serialization, Studio authoring, docs, and examples make state machines approachable.
- Build first-class workflow testing and debugging: test runners for full workflows, breakpoint-like inspection, replay from execution logs where feasible, better failed-activity retry flows, child/descendant workflow instance navigation, and Studio affordances for fault investigation. The Studio `3.7.0` activity call-stack viewer is a useful foundation, but requests for child workflow visibility ([elsa-studio#152](https://github.com/elsa-workflows/elsa-studio/issues/152)) and breakpoint debugging ([elsa-studio discussion #662](https://github.com/elsa-workflows/elsa-studio/discussions/662)) still need a coherent debugging experience.
- Improve Studio authoring fundamentals: input validation ([elsa-studio#15](https://github.com/elsa-workflows/elsa-studio/issues/15)), activity version indicators ([elsa-studio#284](https://github.com/elsa-workflows/elsa-studio/issues/284)), async `/dispatch` instead of blocking `/execute` where appropriate ([elsa-studio#811](https://github.com/elsa-workflows/elsa-studio/issues/811)), designer image export ([elsa-studio#585](https://github.com/elsa-workflows/elsa-studio/issues/585)), and expression evaluation controls ([elsa-studio#643](https://github.com/elsa-workflows/elsa-studio/issues/643)).
- Improve designer extensibility, embedding, and white-labeling: custom activity property editors, custom list actions, embeddable designer/viewer recipes, custom elements, React wrapper direction, auth modes, base-path hosting, branding/theme, and clear Blazor/WASM/Server guidance. Community demand appears in [#4743](https://github.com/elsa-workflows/elsa-core/issues/4743), [#6685](https://github.com/elsa-workflows/elsa-core/issues/6685), [discussion #7246](https://github.com/elsa-workflows/elsa-core/discussions/7246), [elsa-studio#137](https://github.com/elsa-workflows/elsa-studio/issues/137), and [elsa-studio discussion #665](https://github.com/elsa-workflows/elsa-studio/discussions/665).
- Ship user preference and UI state persistence as a Studio platform feature. [elsa-studio#703](https://github.com/elsa-workflows/elsa-studio/issues/703) already scopes theme, layout, table state, local/session storage, reset behavior, and future server profile storage.
- Promote ElsaScript from experiment to useful authoring path: stable syntax, import/export round-tripping, editor diagnostics, examples, and clear boundaries with JSON and visual authoring. See [#7055](https://github.com/elsa-workflows/elsa-core/issues/7055) and the current [`Elsa.Dsl.ElsaScript`](src/modules/Elsa.Dsl.ElsaScript) module.
- Decide the Studio UI framework direction before broad UX work. [elsa-studio#714](https://github.com/elsa-workflows/elsa-studio/issues/714) proposes a MudBlazor-to-Radzen migration, while current work still touches both ecosystems; roadmap work should avoid expensive churn.

Recommended success measures:

- teams with 100+ workflow definitions can find and govern them without naming hacks
- a developer can unit test an activity, integration test a workflow, and debug a failed instance from documented recipes
- common Studio customization no longer requires replacing entire pages or reverse engineering internals
- designer regressions are caught before release across Blazor Server, WASM, and embedded component scenarios

## 3. Integrations And Ecosystem

**Goal:** Elsa should make external systems feel like native workflow building blocks.

High-value items:

- Create an Extension Platform track. Package manifests, shell-feature discovery, Connections/Secrets adoption, generated activity providers, test harness patterns, documentation, and contribution templates should make extensions feel like product-quality packages rather than repo-adjacent samples.
- Create an OpenAPI activity provider that turns an OpenAPI document into typed designer activities. This is a recurring ask in [#2961](https://github.com/elsa-workflows/elsa-core/issues/2961) and [#6360](https://github.com/elsa-workflows/elsa-core/issues/6360), and it is the best foundation for a broad connector story. The existing Extensions OpenAPI work should be reconciled with this product goal.
- Define a connector SDK before adding many one-off integrations. The SDK should cover authentication, secrets, generated activities, testing, metadata, versioning, packaging, Studio property editors, and manifest-driven installation.
- Continue marketplace and plugin infrastructure. The Nuplane modular server, sample package, extension package manifest metadata, and [#7310](https://github.com/elsa-workflows/elsa-core/issues/7310) point to a compelling future where Elsa distributions can install safe custom modules without users maintaining a separate host app.
- Promote Agents to a strategic integration lane: provider matrix, OpenAI and Claude PR resolution, local/OpenRouter/custom endpoints, MCP/tool lifecycle, tool selection in Studio, and auditability. The Extensions source already contains Agents, OpenAI, Azure OpenAI, persistence, API, and Studio modules, while [elsa-extensions#58](https://github.com/elsa-workflows/elsa-extensions/issues/58), [elsa-extensions#98](https://github.com/elsa-workflows/elsa-extensions/pull/98), and [elsa-extensions#63](https://github.com/elsa-workflows/elsa-extensions/pull/63) show the provider matrix and Claude/OpenAI work are still actively moving.
- Prioritize enterprise productivity integrations by leverage: Azure DevOps has a concrete implementation path ([elsa-extensions#124](https://github.com/elsa-workflows/elsa-extensions/issues/124), [elsa-extensions#125](https://github.com/elsa-workflows/elsa-extensions/pull/125)); Teams, OneDrive, SharePoint, Google Docs, and Google Sheets should follow a shared connector model rather than separate bespoke designs.
- Treat dynamic activity generation as a platform primitive. Azure Functions ([elsa-extensions#39](https://github.com/elsa-workflows/elsa-extensions/issues/39)) and registered-method activities ([elsa-extensions#48](https://github.com/elsa-workflows/elsa-extensions/issues/48)) both point toward schema/method-driven activity generation that can also serve OpenAPI, SDK-generated connectors, and internal enterprise APIs.
- Improve data and automation authoring quality: SQL result typing ([elsa-extensions#153](https://github.com/elsa-workflows/elsa-extensions/issues/153), [elsa-extensions#154](https://github.com/elsa-workflows/elsa-extensions/pull/154)), SQL drag/drop reliability ([elsa-extensions#79](https://github.com/elsa-workflows/elsa-extensions/issues/79)), SQL IntelliSense ([elsa-extensions#88](https://github.com/elsa-workflows/elsa-extensions/issues/88)), Dapper package splitting ([elsa-extensions#131](https://github.com/elsa-workflows/elsa-extensions/issues/131), [elsa-extensions#132](https://github.com/elsa-workflows/elsa-extensions/pull/132)), MongoDB secrets parity ([elsa-extensions#126](https://github.com/elsa-workflows/elsa-extensions/issues/126)), WatchFileSystem ([elsa-extensions#90](https://github.com/elsa-workflows/elsa-extensions/issues/90)), and command-line activities ([elsa-extensions#36](https://github.com/elsa-workflows/elsa-extensions/issues/36)).
- Resolve the MassTransit strategy after the v9 licensing change. [discussion #6583](https://github.com/elsa-workflows/elsa-core/discussions/6583) raises a practical ecosystem risk; Elsa should either provide a clean split or reduce dependency weight through a smaller messaging abstraction.
- Clarify Azure Functions and worker-service hosting patterns. [discussion #4707](https://github.com/elsa-workflows/elsa-core/discussions/4707) and [discussion #7420](https://github.com/elsa-workflows/elsa-core/discussions/7420) show demand for non-traditional hosts, Windows services, and serverless-adjacent deployments.
- Add data movement and streaming workflow primitives. [#4809](https://github.com/elsa-workflows/elsa-core/issues/4809) frames this as datasets, linked services, transforms, and stream-oriented processing inspired by Azure Data Factory and stream analytics.
- Treat BPMN as interoperability first, not a wholesale product pivot. [#39](https://github.com/elsa-workflows/elsa-core/issues/39) has strong interest, but the pragmatic first slice is import/export or a constrained BPMN compatibility layer, not full BPMN engine parity.

Recommended success measures:

- a team can connect to a REST API from its OpenAPI spec without hand-building HTTP activities
- marketplace packages can declare features, dependencies, settings, infrastructure needs, and security posture
- integrations are tested with the same rigor as core modules, not shipped as opaque examples
- extension maturity is visible to users as released, main-only, PR open, proposal, or needs-maintainer

## 4. Observability And Operations

**Goal:** Elsa should be easy to inspect from Studio and from standard production telemetry stacks.

High-value items:

- Finish the diagnostics trilogy: structured logs, console logs, and OpenTelemetry. Structured and console logs now exist; Studio `3.8.0-preview1` ships an OpenTelemetry diagnostics page from [elsa-studio#834](https://github.com/elsa-workflows/elsa-studio/pull/834), and Core `main` now includes the OpenTelemetry backend module in [`src/modules/Elsa.Diagnostics.OpenTelemetry`](src/modules/Elsa.Diagnostics.OpenTelemetry). The remaining product work is to release and operationalize that Core backend, document collector setup, and correlate this with workflow incidents.
- Add default workflow semantic metrics: started, resumed, suspended, faulted, completed, active, activity executed/faulted, queue depth, recovery count, drain count, and dispatch latency. [#5988](https://github.com/elsa-workflows/elsa-core/issues/5988) remains the durable demand signal, while [#7537](https://github.com/elsa-workflows/elsa-core/pull/7537) supplies the first current module boundary.
- Build Studio diagnostics pages that are useful under pressure: live console, structured logs, OpenTelemetry traces/metrics/logs, workflow incident timelines, source health, dropped-event counters, source selection, filters, URL state, export/copy affordances, and direct deep links to workflow instances.
- Make execution history easier to reason about: distinguish faulted, interrupted, cancelled, crash-recovered, retried, and operator-modified workflows consistently across API, Studio, logs, and metrics.
- Connect diagnostics to workflow navigation: trace/span IDs, log source IDs, child workflow chains, alterations, runtime admin actions, and recovery actions should be correlated instead of presented as isolated tables.

Recommended success measures:

- local developers can troubleshoot from Studio without opening server logs
- production operators can answer "what is stuck, why, and what changed?" from first-party telemetry
- diagnostic modules have safe redaction defaults and bounded memory behavior

## 5. Security, Identity, And Enterprise Readiness

**Goal:** Elsa should be straightforward to secure in real enterprise hosts.

High-value items:

- Publish canonical OIDC recipes for Blazor Server, WASM, separate server/studio, all-in-one hosts, and reverse-proxy sub-path deployments. Studio `3.7.0` shipped the modern authentication modules, [#7181](https://github.com/elsa-workflows/elsa-core/issues/7181) shows Core-side implementation and documentation demand, and [elsa-studio#809](https://github.com/elsa-workflows/elsa-studio/pull/809) shows sub-path redirect URI handling is still being hardened.
- Provide a production security guide: API keys, JWT/OIDC, default admin bootstrap, scripting trust levels, C# expression risks, Docker demo boundaries, secret masking, tenant isolation, and permission design.
- Expand authorization coverage tests around workflow instances, runtime admin, diagnostics, labels, tenants, and HTTP endpoint activities.
- Add Studio governance controls: tenant/role-based activity visibility, granular permission-aware menus/routes, feature-gated modules, and clear behavior for hidden activities in existing workflow definitions. [elsa-studio#584](https://github.com/elsa-workflows/elsa-studio/issues/584) captures the authoring side of this enterprise need, and new issue [elsa-studio#908](https://github.com/elsa-workflows/elsa-studio/issues/908) sharpens the need for the Studio UI to honor granular permissions consistently.
- Complete localization and white-label readiness: translation contribution docs, coverage status, missing key checks, branding hooks, and supportable customization patterns. Studio issues and discussions show setup/coverage friction in [elsa-studio#771](https://github.com/elsa-workflows/elsa-studio/issues/771), [elsa-studio discussion #695](https://github.com/elsa-workflows/elsa-studio/discussions/695), and [elsa-studio discussion #678](https://github.com/elsa-workflows/elsa-studio/discussions/678).
- Improve multi-tenant ergonomics: tenant-agnostic workflows, high tenant counts, tenant validation modes, cache isolation, and clear migration guidance after the 3.6 tenant ID convention changes.
- Create an enterprise deployment checklist for Kubernetes, reverse proxies/base paths, TLS/custom CAs, database migrations, health checks, backups, and disaster recovery.

Recommended success measures:

- a new enterprise adopter can stand up Elsa behind their identity provider from docs alone
- optional dangerous capabilities are explicit opt-ins with plain-language warnings
- tenant and permission regressions are caught by targeted tests before release

## 6. AI-Assisted Workflow Engineering

**Goal:** AI should make workflows more transparent, not less.

High-value items:

- Build AI-assisted workflow generation that produces multiple visible activities from intent rather than hiding logic in one script activity. This direction is proposed in [discussion #7367](https://github.com/elsa-workflows/elsa-core/discussions/7367), and merged [#7523](https://github.com/elsa-workflows/elsa-core/pull/7523) now provides the first Weaver AI Copilot server foundation with AI abstractions, provider/session contracts, chat/tool endpoints, audit events, proposal persistence, EF Core storage, and integration/unit tests.
- Provide an Elsa MCP/tooling surface for reading, validating, editing, and explaining workflow JSON/ElsaScript. This would make Elsa a strong fit for AI-enabled .NET development environments.
- Align AI authoring with the Extensions Agents work: provider abstractions, MCP tools, OpenAI/Claude/local model support, tool approval, secrets handling, and Studio UX should share contracts instead of creating parallel AI stacks.
- Productize the Studio copilot foundation that landed in [elsa-studio#900](https://github.com/elsa-workflows/elsa-studio/pull/900): proposal review/apply flows, validation, generated activity metadata, designer APIs, diagnostics links, and test scaffolding should be available before AI generation becomes prominent. [elsa-studio#553](https://github.com/elsa-workflows/elsa-studio/issues/553) remains the durable demand signal, and the current Studio workspace still depends on Core exposing more proposal/action endpoints.
- Add "explain this workflow", "find risky activities", "suggest tests", and "generate migration notes" capabilities backed by workflow graph metadata.
- Pair AI generation with validation: generated workflows should include test scaffolds, required input/output definitions, secrets handling, and clear review diffs.

Recommended success measures:

- AI-generated workflows remain inspectable in Studio
- generated workflows come with runnable tests or validation plans
- teams can use AI to refactor workflows without losing visibility, auditability, or review discipline

## Recommended Sequencing

Near term:

1. Finish runtime confidence work: graceful shutdown remaining tasks, recovery clarity, distributed runtime regressions, security documentation, and OIDC recipes.
2. Stabilize Studio authoring: designer regression harness, input/property-editor fixes, async dispatch/run UX, state machine Studio/docs completion, and a clear UI framework direction.
3. Complete the diagnostics trilogy: release and operationalize the Core OpenTelemetry backend, connect it to the Studio OpenTelemetry page, document collector setup, and correlate traces/logs/metrics with workflow incidents.
4. Make workflow authoring easier to manage at scale: organization, search, progress/timeline APIs, testing docs, and user preference/table-state persistence.
5. Reconcile shipped extension foundations with roadmap status: package manifests, Connections/Secrets, OpenAPI, Agents, schedulers, messaging, and integration maturity labels.

Mid term:

1. Promote [#7469](https://github.com/elsa-workflows/elsa-core/issues/7469) from deployment-platform PRD into an implementation spec covering manifests, immutable artifacts, dry-run validation, deployment history, and API/CLI surfaces.
2. OpenAPI activity provider plus connector SDK.
3. Extension Platform: generated activities, contribution harnesses, Studio extension recipes, package manifest maturity, and marketplace/plugin installation path built on Nuplane and shell features.
4. Workflow debugging, replay-oriented incident analysis, child workflow navigation, and operator recovery UX.
5. Weaver AI Copilot productization and Agents provider matrix/MCP lifecycle, with Studio UX that keeps generated workflows inspectable.

Longer term:

1. Native workflow-aware background execution and actor-runtime abstraction.
2. Data pipeline/stream processing primitives.
3. BPMN interoperability.
4. AI-assisted authoring and workflow MCP tools.

## Maintainership Recommendations

- Keep this roadmap in source control and mirror major changes to [#3232](https://github.com/elsa-workflows/elsa-core/issues/3232). Users are explicitly asking for roadmap visibility in [discussion #7202](https://github.com/elsa-workflows/elsa-core/discussions/7202) and the latest roadmap issue comment.
- Use labels, milestones, or GitHub Projects to connect Core, Studio, and Extensions issues to these roadmap themes. Many high-value issues are currently unlabeled, which makes demand hard to see.
- Close or update stale roadmap items that are already implemented, especially state machine, activity testing, ElsaScript, diagnostics, and graceful shutdown foundations.
- Prefer platform primitives over one-off features: connector SDK before many connectors, Extension Platform before bespoke package work, plugin system before bespoke managed extensibility, runtime abstraction before framework-specific actor work.
- Do not hide reliability work under "maintenance". Recovery, distributed correctness, migrations, security posture, and observability are core product features for a workflow engine.
- Enable GitHub Discussions for `elsa-extensions` or explicitly route roadmap discussion into labeled issues/projects. Right now extension demand is visible, but fragmented across issue comments and open pull requests.
